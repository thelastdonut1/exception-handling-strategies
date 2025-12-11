# Result-Based Error Handling

## Core Concept

Instead of throwing exceptions, functions return a `Result<T>` that contains either a success value or an error. Callers must explicitly handle or propagate the result.

```csharp
Result<User> GetUser(int id)
{
    if (id <= 0)
        return Result<User>.Failure("Invalid user ID");
    
    var user = _db.Find(id);
    if (user == null)
        return Result<User>.Failure("User not found");
    
    return Result<User>.Success(user);
}
```

## Key Characteristics

**Explicit in type signature**: `Result<User>` tells you immediately this can fail. No need to read implementation or documentation.

**Manual propagation**: Each layer must check the result and decide to handle or pass it up. Nothing happens automatically.

**Linear control flow**: No jumps. You can read the code top to bottom and understand exactly what happens on failure.

**Structured errors**: Errors can carry codes, context dictionaries, and other metadata—not just a message string.

## Basic Result Type

A minimal implementation:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public class Error
{
    public string Code { get; }
    public string Message { get; }
    public Dictionary<string, object> Context { get; }
}
```

## Composition Patterns

### Bind (FlatMap)
Chain operations that return Results. If any step fails, subsequent steps are skipped:

```csharp
public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> next)
{
    return IsSuccess ? next(Value!) : Result<TNew>.Failure(Error!);
}

// Usage
GetUser(userId)
    .Bind(user => GetOrders(user.Id))
    .Bind(orders => CreateInvoice(orders));
```

### Map
Transform success values without changing the Result structure:

```csharp
public Result<TNew> Map<TNew>(Func<T, TNew> transform)
{
    return IsSuccess ? Result<TNew>.Success(transform(Value!)) : Result<TNew>.Failure(Error!);
}

// Usage
GetOrder(id).Map(order => order.Total);  // Result<Order> -> Result<decimal>
```

### Match
Force exhaustive handling of both cases:

```csharp
public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
{
    return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

// Usage
var response = result.Match(
    onSuccess: order => Ok(order),
    onFailure: error => BadRequest(error.Message)
);
```

## Error Codes vs Exception Types

Instead of an exception hierarchy, use error codes:

```csharp
public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InsufficientInventory = "INSUFFICIENT_INVENTORY";
    public const string DatabaseTimeout = "DB_TIMEOUT";
}

// Handling
switch (error.Code)
{
    case ErrorCodes.NotFound:
        return NotFound();
    case ErrorCodes.ValidationFailed:
        return BadRequest(error.Message);
    default:
        return InternalServerError();
}
```

## Adding Context

Errors can accumulate context as they propagate:

```csharp
var result = _repository.GetItem(itemId);
if (result.IsFailure)
{
    return Result.Failure(
        result.Error
            .WithContext("OrderId", orderId)
            .WithContext("CustomerId", customerId));
}
```

## Converting from Exceptions

Wrap exception-throwing code at boundaries:

```csharp
public static Result<T> Try<T>(Func<T> operation)
{
    try
    {
        return Result<T>.Success(operation());
    }
    catch (Exception ex)
    {
        return Result<T>.Failure(new Error(ex.GetType().Name, ex.Message));
    }
}

// Usage
var result = Result.Try(() => _httpClient.GetAsync(url).Result);
```

## Popular C# Libraries

- **FluentResults**: General-purpose, well-documented
- **ErrorOr**: Lightweight, modern idioms
- **LanguageExt**: Full functional programming toolkit
- **OneOf**: Discriminated unions (more general than Result)