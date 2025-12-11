# Side-by-Side Comparison

## Same Operation, Different Approaches

### Checking Inventory (Repository Layer)

**Exception approach:**
```csharp
public InventoryItem GetItem(int itemId)
{
    try
    {
        connection.Open();
        var results = connection.Query("SELECT ...");
        
        if (results.Count == 0)
            throw new RepositoryException($"Item {itemId} not found");
            
        return MapToInventoryItem(results[0]);
    }
    catch (TimeoutException ex)
    {
        _logger.Error($"Timeout fetching item {itemId}", ex);
        throw new RepositoryException($"Database timeout", ex);
    }
    catch (Exception ex)
    {
        _logger.Error($"Error fetching item {itemId}", ex);
        throw new RepositoryException($"Failed to retrieve item", ex);
    }
}
```

**Result approach:**
```csharp
public Result<InventoryItem> GetItem(int itemId)
{
    var queryResult = connection.Query("SELECT ...");
    
    if (queryResult.IsFailure)
        return Result<InventoryItem>.Failure(
            queryResult.Error.WithContext("ItemId", itemId));
    
    if (queryResult.Data.Count == 0)
        return Result<InventoryItem>.Failure(
            new Error("Item not found", ErrorCodes.NotFound)
                .WithContext("ItemId", itemId));
    
    return Result<InventoryItem>.Success(
        MapToInventoryItem(queryResult.Data[0]));
}
```

---

### Handling Errors (Controller Layer)

**Exception approach:**
```csharp
public ApiResponse<Order> HandlePlaceOrder(string customerId, List<OrderItem> items)
{
    try
    {
        var order = _orderService.PlaceOrder(customerId, items);
        return ApiResponse.Ok(order);
    }
    catch (OrderException ex)
    {
        _logger.Error("Order failed", ex);
        
        // Walk the InnerException chain to find root cause
        var message = GetUserFriendlyMessage(ex);
        return ApiResponse.Error(message);
    }
    catch (Exception ex)
    {
        _logger.Error("Unexpected error", ex);
        return ApiResponse.Error("Something went wrong");
    }
}

private string GetUserFriendlyMessage(OrderException ex)
{
    if (ex.InnerException is InventoryException inv)
    {
        if (inv.Message.Contains("Insufficient"))
            return $"Not enough stock for item {inv.ItemId}";
    }
    return "Could not complete order";
}
```

**Result approach:**
```csharp
public ApiResponse<Order> HandlePlaceOrder(string customerId, List<OrderItem> items)
{
    var result = _orderService.PlaceOrder(customerId, items);
    
    return result.Match(
        onSuccess: order => ApiResponse.Ok(order),
        onFailure: error =>
        {
            _logger.Error($"Order failed: {error}");
            return ApiResponse.Error(GetUserFriendlyMessage(error));
        });
}

private string GetUserFriendlyMessage(Error error)
{
    return error.Code switch
    {
        ErrorCodes.InsufficientInventory => 
            $"Not enough stock for item {error.Context["ItemId"]}",
        ErrorCodes.ItemNotFound => 
            $"Item {error.Context["ItemId"]} not available",
        _ => "Could not complete order"
    };
}
```

---

## When to Use Each

### Use Exceptions When:
- Integrating with .NET libraries that throw
- Handling truly exceptional cases (out of memory, stack overflow)
- Programming errors that indicate bugs (null reference, index out of bounds)
- Most layers genuinely can't handle the error

### Use Results When:
- Failures are expected business outcomes (validation, insufficient stock)
- Callers should always handle the failure case
- You want structured error information
- You need clean partial failure handling
- You want to log once at the boundary, not at every layer

### Hybrid Approach
Many codebases use both:
- Results for internal domain logic
- Convert to exceptions at public API boundaries (if matching .NET conventions)
- Wrap external exceptions in Results at infrastructure boundaries