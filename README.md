# Error Handling Patterns in C#

A side-by-side reference comparing **exception-based** and **Result-based** error handling in C#.

## Repository Structure

```
├── ExceptionDemo/          # Exception-based approach
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Database/
│   ├── Exceptions/
│   └── Models/
├── ResultDemo/             # Result-based approach
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Database/
│   ├── Common/
│   └── Models/
└── docs/
    ├── exception-approach.md
    ├── result-approach.md
    └── comparison.md
```

## The Scenario

Both demos model the same order placement flow:

```
Controller.HandlePlaceOrder()
  → OrderService.PlaceOrder()
    → InventoryService.ReserveItems()
      → InventoryRepository.UpdateQuantity()
        → Database.Execute()
```

Each layer can fail. The demos show how errors propagate through this stack in each paradigm.

## Quick Comparison

| Aspect | Exceptions | Results |
|--------|------------|---------|
| Error visibility | Hidden in implementation | Explicit in return type |
| Propagation | Automatic (throw/catch) | Manual (check and return) |
| Logging | Often duplicated per layer | Single point at boundary |
| Business failures | Same mechanism as system errors | Distinct from infrastructure errors |

## Further Reading

See `/docs` for detailed explanations of each approach.