using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWebV01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var totalOrders = _unitOfWork.OrderHeader.GetAll().Count();
            var totalProducts = _unitOfWork.Product.GetAll().Count();
            var totalUsers = _unitOfWork.ApplicationUser.GetAll().Count();
            var totalRevenue = _unitOfWork.OrderHeader.GetAll()
                .Where(o => o.OrderStatus == SD.StatusApproved || o.OrderStatus == SD.StatusShipped)
                .Sum(o => o.OrderTotal);

            var pendingOrders = _unitOfWork.OrderHeader.GetAll()
                .Where(o => o.OrderStatus == SD.StatusPending)
                .Count();

            var recentOrders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }
    }
}
