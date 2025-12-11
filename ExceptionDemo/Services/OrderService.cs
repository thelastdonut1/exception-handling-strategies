using OrderSystem.ExceptionDemo.Exceptions;
using OrderSystem.ExceptionDemo.Repositories;
using OrderSystem.Models;

namespace OrderSystem.ExceptionDemo.Services;

public class OrderService
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger _logger;
    private static int _nextOrderId = 1;

    public OrderService(InventoryService inventoryService, ILogger logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public Order PlaceOrder(string customerId, List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID is required", nameof(customerId));

        if (items == null || items.Count == 0)
            throw new ArgumentException("Order must contain at least one item", nameof(items));

        var order = new Order
        {
            Id = _nextOrderId++,
            CustomerId = customerId,
            Items = items,
            Status = OrderStatus.Pending
        };

        _logger.Info($"Created order {order.Id} for customer {customerId}");

        try
        {
            _inventoryService.ReserveItems(items);
            order.Status = OrderStatus.Reserved;
            
            order.Status = OrderStatus.Confirmed;
            _logger.Info($"Order {order.Id} confirmed");

            return order;
        }
        catch (InventoryException ex)
        {
            _logger.Error($"Failed to place order {order.Id}: inventory reservation failed", ex); // Log #3 (duplicate)
            order.Status = OrderStatus.Failed;

            // Exception chain: OrderException -> InventoryException -> RepositoryException -> DataException
            throw new OrderException($"Order {order.Id} failed: could not reserve inventory", ex,
                orderId: order.Id, customerId: customerId);
        }
        catch (ArgumentException)
        {
            throw; // Programming errors - let bubble up
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error placing order {order.Id}", ex);
            order.Status = OrderStatus.Failed;
            throw new OrderException($"Order {order.Id} failed due to an unexpected error", ex,
                orderId: order.Id, customerId: customerId);
        }
    }
}