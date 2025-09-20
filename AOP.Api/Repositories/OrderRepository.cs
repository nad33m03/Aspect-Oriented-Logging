using AOP.Api.Models;

namespace AOP.Api.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new();
        private int _nextId = 1;

        public Order? GetOrder(int id) => _orders.FirstOrDefault(o => o.Id == id);

        public IEnumerable<Order> GetOrders() => _orders;

        public void AddOrder(Order order)
        {
            order.Id = _nextId++;
            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            _orders.Add(order);
        }
    }
}
