# Exception-Based Error Handling

## Philosophy

Errors are interruptions to normal program flow. Business logic should read cleanly without error-handling clutter, and errors propagate automatically until something catches them.

## How It Works

When an error occurs, you `throw` an exception. It travels up the call stack until a `catch` block handles it—or crashes the program.

```
Database throws DataException
    ↓ (uncaught, propagates up)
Repository catches, wraps in RepositoryException, rethrows
    ↓ (uncaught, propagates up)  
Service catches, wraps in InventoryException, rethrows
    ↓ (uncaught, propagates up)
Controller catches, converts to API response
```

## Layer-by-Layer Patterns

### Database Layer
Throws raw exceptions (SqlException, TimeoutException, etc.)

### Repository Layer
- Catches database exceptions
- Wraps in domain-specific exception (RepositoryException)
- Adds context (entity type, ID)
- Often logs here
- Rethrows

### Service Layer
- Catches repository exceptions
- Wraps in business exception (InventoryException, OrderException)
- Adds business context
- Often logs here (duplicate!)
- Rethrows

### Controller Layer
- Catches all exception types
- Converts to user-friendly response
- Logs (third time!)
- Must walk InnerException chain to determine root cause

## Key Characteristics

**Automatic propagation**: Don't need to explicitly pass errors up—they bubble automatically.

**Non-local control flow**: A throw in the repository can jump directly to a catch in the controller, skipping intermediate layers.

**Exception hierarchies**: Different exception types (RepositoryException, InventoryException) for different layers.

**Hidden failure modes**: Method signature doesn't reveal what can fail.

## Common Issues

1. **Multiple log entries**: Each catch block often logs, creating duplicate entries for one failure.

2. **String matching for error types**: Controllers often check `ex.Message.Contains("...")` to determine error type—fragile.

3. **Business cases as exceptions**: "Insufficient inventory" uses the same mechanism as database failures.

4. **Unclear catch responsibilities**: Which layer should catch? Log? Wrap? Swallow?