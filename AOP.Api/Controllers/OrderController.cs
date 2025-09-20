using Microsoft.AspNetCore.Mvc;
using AOP.Api.Models;
using AOP.Api.Services;

namespace AOP.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Order> GetOrders()
        {
            return _orderService.GetOrders();
        }

        [HttpGet("{id}")]
        public ActionResult<Order> GetOrder(int id)
        {
            var order = _orderService.GetOrder(id);
            if (order == null)
                return NotFound();
            return order;
        }

        [HttpPost]
        public IActionResult CreateOrder([FromBody] Order order)
        {
            _orderService.CreateOrder(order);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
    }
}
