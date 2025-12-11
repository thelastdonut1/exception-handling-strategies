namespace OrderSystem.ExceptionDemo.Exceptions;

public class RepositoryException : Exception
{
    public string? EntityType { get; }
    public object? EntityId { get; }

    public RepositoryException(string message, Exception? innerException = null, 
        string? entityType = null, object? entityId = null)
        : base(message, innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

public class InventoryException : Exception
{
    public int? ItemId { get; }
    public int? RequestedQuantity { get; }

    public InventoryException(string message, Exception? innerException = null,
        int? itemId = null, int? requestedQuantity = null)
        : base(message, innerException)
    {
        ItemId = itemId;
        RequestedQuantity = requestedQuantity;
    }
}

public class OrderException : Exception
{
    public int? OrderId { get; }
    public string? CustomerId { get; }

    public OrderException(string message, Exception? innerException = null,
        int? orderId = null, string? customerId = null)
        : base(message, innerException)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }
}