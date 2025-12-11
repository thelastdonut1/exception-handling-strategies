# Result-Based Error Handling

## Philosophy

Errors are values, not interruptions. A function's return type should honestly represent all possible outcomes—including failure. Callers must explicitly acknowledge that failure is possible.

## How It Works

Instead of throwing, functions return a `Result<T>` that contains either a success value or an error. Each layer checks the result and decides whether to handle it, add context, or pass it up.

```
Database returns Result.Failure(error)
    ↓ (checked, context added)
Repository returns Result.Failure(error.WithContext(...))
    ↓ (checked, context added)
Service returns Result.Failure(error.Wrap(...))
    ↓ (checked)
Controller logs once, converts to API response
```

## Layer-by-Layer Patterns

### Database Layer
Returns `Result<T>` with error codes (DatabaseTimeout, DatabaseDeadlock, etc.)

### Repository Layer
- Checks if database result failed
- Adds context (entity type, ID) via `.WithContext()`
- Returns the enriched error—no wrapping in new types needed

### Service Layer
- Checks if repository result failed
- Adds business context
- Returns—no logging here

### Controller Layer
- Calls `.Match()` to handle success/failure
- Logs once at this boundary
- Switches on error codes to build user message
- Structured context available via dictionary

## Key Characteristics

**Explicit in type signature**: `Result<Order>` tells you this can fail.

**Linear control flow**: No jumps—errors flow through return values.

**Error codes over hierarchies**: Use string codes (`INSUFFICIENT_INVENTORY`) instead of exception class hierarchies.

**Structured context**: Errors carry a `Dictionary<string, object>` of relevant data.

## Composition Patterns

**Bind**: Chain operations that return Results
```csharp
GetUser(id)
    .Bind(user => GetOrders(user.Id))
    .Bind(orders => CreateInvoice(orders))
```

**Map**: Transform success values
```csharp
GetOrder(id).Map(order => order.Total)
```

**Match**: Force handling of both cases
```csharp
result.Match(
    onSuccess: order => Ok(order),
    onFailure: error => BadRequest(error.Message)
)
```

## Key Benefits

1. **Single logging point**: Log at the boundary (controller), not at every layer.

2. **Error codes for branching**: Switch on codes instead of matching exception message strings.

3. **Business cases are just results**: "Insufficient inventory" is a `Result.Failure` with a specific code—not disguised as a system error.

4. **Explicit partial failure handling**: You can see exactly what succeeded before a failure.