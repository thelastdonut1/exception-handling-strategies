using OrderSystem.ResultDemo.Common;
using OrderSystem.ResultDemo.Services;
using OrderSystem.Models;

namespace OrderSystem.ResultDemo.Controllers;

/// <summary>
/// No try-catch blocks. Uses Match for exhaustive handling. Logs once here at the boundary.
/// </summary>
public class OrderController
{
    private readonly OrderService _orderService;
    private readonly ILogger _logger;

    public OrderController(OrderService orderService, ILogger logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public ApiResponse<Order> HandlePlaceOrder(string customerId, List<OrderItem> items)
    {
        var result = _orderService.PlaceOrder(customerId, items);

        return result.Match(
            onSuccess: order => new ApiResponse<Order>
            {
                Success = true,
                Data = order,
                Message = "Order placed successfully"
            },
            onFailure: error =>
            {
                _logger.Error($"Order placement failed: {error}"); // Single log entry
                return new ApiResponse<Order>
                {
                    Success = false,
                    Message = GetUserFriendlyMessage(error),
                    ErrorCode = error.Code
                };
            });
    }

    /// <summary>
    /// Switch on error codes, pull from structured context. No string matching.
    /// </summary>
    private string GetUserFriendlyMessage(Error error)
    {
        switch (error.Code)
        {
            case ErrorCodes.InsufficientInventory:
                var itemId = error.Context.GetValueOrDefault("ItemId", "unknown");
                var requested = error.Context.GetValueOrDefault("RequestedQuantity", "unknown");
                var available = error.Context.GetValueOrDefault("AvailableQuantity", "unknown");
                return $"Sorry, not enough stock for item {itemId}. Requested {requested}, available {available}.";

            case ErrorCodes.ItemNotFound:
                return $"Item {error.Context.GetValueOrDefault("ItemId", "unknown")} is not available.";

            case ErrorCodes.ValidationError:
                return error.Message;

            case ErrorCodes.DatabaseTimeout:
                return "System is experiencing high load. Please try again.";

            case ErrorCodes.DatabaseDeadlock:
            case ErrorCodes.DatabaseConnection:
            case ErrorCodes.DatabaseError:
                return "We encountered a problem processing your order. Please try again.";

            case ErrorCodes.OrderFailed:
                return error.InnerError != null 
                    ? GetUserFriendlyMessage(error.InnerError) 
                    : "Could not complete order.";

            default:
                return "Could not complete order. Please try again or contact support.";
        }
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public class ConsoleLogger : ILogger
{
    private readonly string _category;
    public ConsoleLogger(string category) => _category = category;
    public void Info(string message) => Console.WriteLine($"[{_category}] INFO: {message}");
    public void Warning(string message) => Console.WriteLine($"[{_category}] WARN: {message}");
    public void Error(string message) => Console.WriteLine($"[{_category}] ERROR: {message}");
}