# About This Example

## The Scenario

Both demos implement the same order placement flow:

```
Controller.HandlePlaceOrder()
  → OrderService.PlaceOrder()
    → InventoryService.ReserveItems()
      → InventoryRepository.UpdateQuantity()
        → Database.Execute()
```

This is a typical layered architecture: Controller → Service → Repository → Database. Each layer can fail, and errors must somehow reach the controller to become an API response.

## What the Code Shows

### ExceptionDemo

The exception-based implementation demonstrates:

**Exception wrapping at each layer**
```
DataException (database)
  → RepositoryException (repository catches, wraps, rethrows)
    → InventoryException (service catches, wraps, rethrows)
      → OrderException (service catches, wraps, rethrows)
```

**Logging at multiple layers**

Each catch block logs before rethrowing. Comments mark these as `// Log #1`, `// Log #2`, etc. to highlight the duplication.

**String matching for error types**

The controller's `GetUserFriendlyMessage()` method checks `ex.Message.Contains("Insufficient")` to determine error types—a fragile pattern.

**Business cases as exceptions**

"Insufficient inventory" is a normal business outcome, but it uses the same exception mechanism as database failures.

### ResultDemo

The Result-based implementation demonstrates:

**No try-catch blocks in service/repository layers**

Errors flow through return values. The code reads linearly.

**Single logging point**

Only the controller has a logger. Errors are logged once at the boundary.

**Error codes for branching**

The controller switches on `error.Code` rather than matching exception message strings.

**Structured context**

Errors carry a `Context` dictionary with typed values like `ItemId` and `RequestedQuantity`.

**Explicit partial failure handling**

The `InventoryService.ReserveItems()` method tracks which items were reserved and actually implements rollback on failure.

## Side-by-Side: Repository Layer

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

## Side-by-Side: Controller Layer

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

## Important Caveat

This example was designed to highlight differences between the approaches in a scenario involving multiple layers and expected business failures. This plays to Result's strengths.

See `analysis.md` for a balanced comparison of when each approach is more appropriate.