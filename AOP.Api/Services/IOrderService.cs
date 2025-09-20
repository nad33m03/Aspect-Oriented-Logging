using AOP.Api.Models;

namespace AOP.Api.Services
{
    public interface IOrderService
    {
        Order? GetOrder(int id);
        IEnumerable<Order> GetOrders();
        void CreateOrder(Order order);
    }
}
