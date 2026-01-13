using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using BulkyWebV01.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace BulkyWebV01.Areas.Customer.Controllers
{

    [Area("customer")]
    [Authorize]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        private readonly IEmailSender _emailSender;
        private readonly PaymobClient _paymobClient;
        private readonly PaymobSettings _paymobSettings;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; } 

        public CartController (IUnitOfWork unitOfWork,  IEmailSender emailSender, PaymobClient paymobClient, IOptions<PaymobSettings> paymobOptions)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _paymobClient = paymobClient;
            _paymobSettings = paymobOptions.Value;
        }

        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties:"Product"),
                OrderHeader = new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
             cart.Product.ProductImages = productImages.Where(u=>u.ProductId == cart.Product.Id).ToList();
             cart. Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            // Do not pre-fill address fields - require user to enter shipping details for each order
            // ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            // ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            // ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            // ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            // ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            // ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }


        [HttpPost]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST()
		{

			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
            includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;


			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);


			//ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			//ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			//ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			//ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			//ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			//ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;




			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}


            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer account and we need to capture payment
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;

			}
            else
            {
				// it is a company user.
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;

			}

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach(var cart in ShoppingCartVM.ShoppingCartList) {

                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count

                };
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();

			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer account and we need to capture payment
                
                // Check if Paymob is properly configured
                if (_paymobSettings.IntegrationId > 0 && !string.IsNullOrWhiteSpace(_paymobSettings.ApiKey))
                {
                    var orderId = ShoppingCartVM.OrderHeader.Id;
                    var amountCents = (long)Math.Round(ShoppingCartVM.OrderHeader.OrderTotal * 100);

                    var (firstName, lastName) = SplitName(ShoppingCartVM.OrderHeader.Name);
                    var billing = new PaymobBillingData(
                        Apartment: "NA",
                        Email: applicationUser.Email ?? "na@example.com",
                        Floor: "NA",
                        FirstName: firstName,
                        LastName: lastName,
                        Street: ShoppingCartVM.OrderHeader.StreetAddress,
                        Building: "NA",
                        PhoneNumber: ShoppingCartVM.OrderHeader.PhoneNumber,
                        ShippingMethod: "PKG",
                        PostalCode: ShoppingCartVM.OrderHeader.PostalCode,
                        City: ShoppingCartVM.OrderHeader.City,
                        State: ShoppingCartVM.OrderHeader.State,
                        Country: "EG"
                    );

                    var items = ShoppingCartVM.ShoppingCartList
                        .Select(i => new PaymobOrderItem(
                            Name: i.Product.Title,
                            AmountCents: (long)Math.Round(i.Price * 100),
                            Quantity: i.Count,
                            Description: null
                        ))
                        .ToList();

                    var paymobRequest = new PaymobCreatePaymentRequest(
                        AmountCents: amountCents,
                        Currency: string.IsNullOrWhiteSpace(_paymobSettings.Currency) ? "EGP" : _paymobSettings.Currency,
                        MerchantOrderId: $"BULKY-{orderId}",
                        BillingData: billing,
                        Items: items
                    );

                    var paymentInit = await _paymobClient.CreatePaymentAsync(paymobRequest);

                    // Store Paymob order reference in SessionId (field name kept for backward compatibility)
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderId, sessionId: paymentInit.PaymobOrderId.ToString(), paymentIntentId: "");
                    _unitOfWork.Save();

                    return Redirect(paymentInit.PaymentUrl);
                }
                else
                {
                    // Paymob not configured - treat as Cash On Delivery for testing
                    TempData["warning"] = "Payment gateway not configured. Order placed as Cash On Delivery.";
                    _unitOfWork.OrderHeader.UpdateStatus(ShoppingCartVM.OrderHeader.Id, SD.StatusApproved, SD.PaymentStatusPending);
                    _unitOfWork.Save();
                }
			}

            await FinalizeOrderAsync(ShoppingCartVM.OrderHeader.Id);
            return RedirectToAction(nameof(OrderConfirmation) , new {id = ShoppingCartVM.OrderHeader.Id});
		}

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PaymobReturn(int orderId, long? transactionId, bool? success)
        {
            if (transactionId == null || success != true)
            {
                TempData["error"] = "Payment was not completed.";
                return RedirectToAction(nameof(Index));
            }

            var transaction = await _paymobClient.GetTransactionAsync(transactionId.Value);
            if (transaction == null || transaction.Success != true)
            {
                TempData["error"] = "Payment verification failed.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.OrderHeader.UpdateStripePaymentID(orderId, sessionId: "", paymentIntentId: transaction.Id.ToString());
            _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusApproved, SD.PaymentStatusApproved);
            _unitOfWork.Save();

            await FinalizeOrderAsync(orderId);
            return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
        }

        private async Task FinalizeOrderAsync(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser");
            if (orderHeader == null)
            {
                return;
            }

            await _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - E-Commerce",
                $"<p>New Order Created - {orderHeader.Id}</p>");

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
        }

        private static (string FirstName, string LastName) SplitName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return ("Customer", "");
            }

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (parts[0], "");
            }

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }



		public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u=>u.Id==cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }
        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            if (cartFromDb.Count <= 1)
            {
                //remove that from cart
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
                
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            
        }


        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked:true);
         
               

            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);


            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }


        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            // Use SalePrice if available, otherwise use regular Price
            if (shoppingCart.Product.SalePrice.HasValue && shoppingCart.Product.SalePrice.Value > 0)
            {
                return shoppingCart.Product.SalePrice.Value;
            }
            return shoppingCart.Product.Price;
        }

    }
}
