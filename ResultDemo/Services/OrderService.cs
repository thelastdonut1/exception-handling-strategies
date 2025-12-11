using OrderSystem.ResultDemo.Common;
using OrderSystem.Models;

namespace OrderSystem.ResultDemo.Services;

/// <summary>
/// No try-catch blocks. No logging. Results compose cleanly.
/// </summary>
public class OrderService
{
    private readonly InventoryService _inventoryService;
    private static int _nextOrderId = 1;

    public OrderService(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Result<Order> PlaceOrder(string customerId, List<OrderItem> items)
    {
        // Validation returns Result, not ArgumentException
        var validationResult = ValidateOrderRequest(customerId, items);
        if (validationResult.IsFailure)
            return Result<Order>.Failure(validationResult.Error!);

        var order = new Order
        {
            Id = _nextOrderId++,
            CustomerId = customerId,
            Items = items,
            Status = OrderStatus.Pending
        };

        var reserveResult = _inventoryService.ReserveItems(items);

        if (reserveResult.IsFailure)
        {
            order.Status = OrderStatus.Failed;
            return Result<Order>.Failure(
                reserveResult.Error!
                    .WithContext("OrderId", order.Id)
                    .WithContext("CustomerId", customerId)
                    .Wrap($"Order {order.Id} failed", ErrorCodes.OrderFailed));
        }

        order.Status = OrderStatus.Confirmed;
        return Result<Order>.Success(order);
    }

    private Result ValidateOrderRequest(string customerId, List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result.Failure(
                new Error("Customer ID is required", ErrorCodes.ValidationError)
                    .WithContext("Field", "CustomerId"));
        }

        if (items == null || items.Count == 0)
        {
            return Result.Failure(
                new Error("Order must contain at least one item", ErrorCodes.ValidationError)
                    .WithContext("Field", "Items"));
        }

        return Result.Success();
    }
}