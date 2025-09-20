using AOP.Api.Models;
using AOP.Api.Repositories;

namespace AOP.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public Order? GetOrder(int id) => _orderRepository.GetOrder(id);

        public IEnumerable<Order> GetOrders() => _orderRepository.GetOrders();

        public void CreateOrder(Order order) => _orderRepository.AddOrder(order);
    }
}
