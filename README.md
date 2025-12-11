# Error Handling Patterns in C#

A side-by-side reference comparing **exception-based** and **Result-based** error handling in C#.

## Repository Structure

```
├── ExceptionDemo/          # Exception-based implementation
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Database/
│   ├── Exceptions/
│   └── Models/
├── ResultDemo/             # Result-based implementation
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Database/
│   ├── Common/
│   └── Models/
└── docs/
    ├── exceptions.md       # Exception pattern explained
    ├── results.md          # Result pattern explained
    ├── example.md          # This repo's example explained
    └── analysis.md         # Balanced comparison & recommendations
```

## The Example

Both demos implement the same order placement flow:

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
| Propagation | Automatic (throw/catch) | Manual (check and return) |
| Handling | Opt-in (catch if you want) | Forced (must acknowledge) |
| Type signature | Hides failure modes | Explicit: `Result<T>` |
| Best for | Rare failures, deep stacks, .NET interop | Expected failures, business logic, structured errors |

## Documentation

| Document | Contents |
|----------|----------|
| [exceptions.md](docs/exceptions.md) | How exception handling works, common patterns, best practices |
| [results.md](docs/results.md) | How Result types work, Bind/Map/Match patterns, libraries |
| [example.md](docs/example.md) | Walkthrough of this repository's code, side-by-side comparisons |
| [analysis.md](docs/analysis.md) | Balanced comparison, when to use each, industry landscape, recommendations |

Start with `analysis.md` for the big picture, then explore the pattern-specific docs as needed.