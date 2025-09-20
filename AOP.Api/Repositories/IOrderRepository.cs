using AOP.Api.Models;

namespace AOP.Api.Repositories
{
    public interface IOrderRepository
    {
        Order? GetOrder(int id);
        IEnumerable<Order> GetOrders();
        void AddOrder(Order order);
    }
}
