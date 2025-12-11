using OrderSystem.ExceptionDemo.Exceptions;
using OrderSystem.ExceptionDemo.Repositories;
using OrderSystem.ExceptionDemo.Services;
using OrderSystem.Models;

namespace OrderSystem.ExceptionDemo.Controllers;

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
        try
        {
            var order = _orderService.PlaceOrder(customerId, items);
            return new ApiResponse<Order> { Success = true, Data = order, Message = "Order placed successfully" };
        }
        catch (OrderException ex)
        {
            _logger.Error($"Order placement failed", ex); // Log #4 (duplicate)
            var userMessage = GetUserFriendlyMessage(ex);
            return new ApiResponse<Order> { Success = false, Message = userMessage, ErrorCode = "ORDER_FAILED" };
        }
        catch (ArgumentException ex)
        {
            _logger.Warning($"Invalid order request: {ex.Message}");
            return new ApiResponse<Order> { Success = false, Message = ex.Message, ErrorCode = "INVALID_REQUEST" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Unhandled exception in HandlePlaceOrder", ex);
            return new ApiResponse<Order> { Success = false, Message = "An unexpected error occurred", ErrorCode = "INTERNAL_ERROR" };
        }
    }

    /// <summary>
    /// Must walk InnerException chain and match on message strings to determine error type.
    /// </summary>
    private string GetUserFriendlyMessage(OrderException ex)
    {
        if (ex.InnerException is InventoryException invEx)
        {
            if (invEx.Message.Contains("Insufficient"))
            {
                return $"Sorry, we don't have enough stock for item {invEx.ItemId}. " +
                       $"You requested {invEx.RequestedQuantity}.";
            }

            if (invEx.Message.Contains("does not exist"))
            {
                return $"Item {invEx.ItemId} is not available.";
            }

            if (invEx.InnerException is RepositoryException repoEx)
            {
                if (repoEx.InnerException is TimeoutException)
                {
                    return "Our system is experiencing high load. Please try again in a few moments.";
                }

                return "We encountered a problem processing your order. Please try again.";
            }
        }

        return "We couldn't complete your order. Please try again or contact support.";
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}