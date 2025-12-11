# Exception-Based Error Handling

## Core Concept

When an error occurs, you `throw` an exception. It propagates up the call stack automatically until a `catch` block handles it—or terminates the program.

```csharp
void LayerA() 
{
    LayerB(); // If LayerB throws and we don't catch, it propagates up
}

void LayerB() 
{
    throw new InvalidOperationException("Something went wrong");
}
```

## Key Characteristics

**Automatic propagation**: Errors bubble up without explicit handling at each layer. Intermediate code doesn't need to know about errors passing through.

**Non-local control flow**: A throw can jump many frames up the stack to reach its catch. This is powerful but means you can't understand error flow by reading code linearly.

**Stack traces**: Exceptions capture where they originated, making debugging easier.

**Hidden in signatures**: A method signature doesn't reveal what exceptions it might throw (unlike Java's checked exceptions).

## Common Patterns

### Catch and Wrap
Add context at each layer by wrapping in a new exception type:

```csharp
catch (SqlException ex)
{
    throw new RepositoryException("Failed to fetch user", ex);
}
```

### Catch and Log
Log the error, then rethrow to let it continue propagating:

```csharp
catch (Exception ex)
{
    _logger.Error(ex, "Operation failed");
    throw;
}
```

### Catch and Handle
Actually handle the error, preventing further propagation:

```csharp
catch (FileNotFoundException)
{
    return GetDefaultConfig();
}
```

### Exception Filters (C# 6+)
Catch only when a condition is met:

```csharp
catch (SqlException ex) when (ex.Number == 1205) // Deadlock
{
    return Retry();
}
```

## Exception Hierarchies

Custom exceptions typically form a hierarchy:

```
Exception
└── ApplicationException
    └── RepositoryException
    └── ServiceException
        └── InventoryException
        └── OrderException
```

This allows catching at different granularities:
- `catch (OrderException)` — specific
- `catch (ServiceException)` — any service error  
- `catch (Exception)` — everything

## Best Practices

1. **Throw on programming errors**: Null arguments, invalid state, index out of bounds. These are bugs—fail fast.

2. **Use specific exception types**: Don't just throw `Exception`. Create or use specific types that callers can catch selectively.

3. **Preserve stack traces**: Use `throw;` not `throw ex;` when rethrowing.

4. **Include context in messages**: "Failed to update user 42" is better than "Update failed".

5. **Don't catch and swallow silently**: If you catch, either handle meaningfully or rethrow.

6. **Catch at boundaries**: Controllers, message handlers, job runners—places where you need to convert to a response or log and continue.