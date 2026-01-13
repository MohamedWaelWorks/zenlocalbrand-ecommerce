using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BulkyWebV01.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWebV01.Areas.Admin.Controllers
{
	[Area("admin")]
    [Authorize]
	public class OrderController : Controller
	{

		private readonly IUnitOfWork _unitOfWork;
        private readonly PaymobClient _paymobClient;
        private readonly PaymobSettings _paymobSettings;

        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork, PaymobClient paymobClient, IOptions<PaymobSettings> paymobOptions)
		{
			_unitOfWork = unitOfWork;
            _paymobClient = paymobClient;
            _paymobSettings = paymobOptions.Value;

		}


		public IActionResult Index()
		{
			return View();
		}


        public IActionResult Details(int orderId)
        {
            OrderVM  = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(int orderId)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress= OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new {orderId= orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        

        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult ShipOrder()
        {
             var orderHeader = _unitOfWork.OrderHeader.Get(u=>u.Id ==  OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }


            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            TempData["success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });


        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            // NOTE: Refund via Paymob API can be added once you decide which Paymob payment action endpoint
            // you want to use (refund/void/auth-capture). For now we keep the same status behavior.
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }


        [ActionName("Details")]
        [HttpPost]
        public async Task<IActionResult> Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
               .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            var amountCents = (long)Math.Round(OrderVM.OrderHeader.OrderTotal * 100);
            var (firstName, lastName) = SplitName(OrderVM.OrderHeader.Name);

            var billing = new PaymobBillingData(
                Apartment: "NA",
                Email: OrderVM.OrderHeader.ApplicationUser.Email ?? "na@example.com",
                Floor: "NA",
                FirstName: firstName,
                LastName: lastName,
                Street: OrderVM.OrderHeader.StreetAddress,
                Building: "NA",
                PhoneNumber: OrderVM.OrderHeader.PhoneNumber,
                ShippingMethod: "PKG",
                PostalCode: OrderVM.OrderHeader.PostalCode,
                City: OrderVM.OrderHeader.City,
                State: OrderVM.OrderHeader.State,
                Country: "EG"
            );

            var items = OrderVM.OrderDetail
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
                MerchantOrderId: $"BULKY-ADMIN-{OrderVM.OrderHeader.Id}",
                BillingData: billing,
                Items: items
            );

            var paymentInit = await _paymobClient.CreatePaymentAsync(paymobRequest);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, sessionId: paymentInit.PaymobOrderId.ToString(), paymentIntentId: "");
            _unitOfWork.Save();
            return Redirect(paymentInit.PaymentUrl);
     
        }



        public async Task<IActionResult> PaymentConfirmation(int orderHeaderId, long? transactionId, bool? success)
        {
            if (transactionId == null || success != true)
            {
                TempData["error"] = "Payment was not completed.";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderId });
            }

            var transaction = await _paymobClient.GetTransactionAsync(transactionId.Value);
            if (transaction == null || transaction.Success != true)
            {
                TempData["error"] = "Payment verification failed.";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderId });
            }

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, sessionId: "", paymentIntentId: transaction.Id.ToString());
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
            _unitOfWork.Save();
            return View(orderHeaderId);
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









        #region API CALLS

        [HttpGet]

		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            if(User.IsInRole(SD.Role_Admin)|| User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties:"ApplicationUser").ToList();
            }
            else
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId  == userId, includeProperties: "ApplicationUser");





            }

            switch (status)
            {
                case "pending":
					objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;

                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;

                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;

                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;

                default:
					break;
            }

            return Json(new { data = objOrderHeaders });

		}

		

		


		#endregion


	}
}
