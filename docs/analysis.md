# Analysis: Exceptions vs Results

## The Fundamental Tradeoff

Exceptions and Results solve the same problem with inverted defaults:

| Approach | Propagation | Handling |
|----------|-------------|----------|
| Exceptions | Automatic (opt-out) | Opt-in (catch if you want) |
| Results | Manual (must propagate) | Forced (must acknowledge) |

Neither is inherently better. The question is which default serves your specific context.

---

## Where Exceptions Excel

### Deep Call Stacks

If you have 15 layers between failure and handler, exceptions just teleport there. Results require every intermediate layer to check and propagate:

```csharp
// With Results, every layer needs this ceremony:
var result = _inner.DoThing();
if (result.IsFailure) return Result.Failure(result.Error);

// With exceptions: nothing. The error bubbles automatically.
```

For deep architectures where most layers genuinely can't handle errors, this ceremony adds noise without value.

### Truly Exceptional Failures

Out of memory. Stack overflow. Thread abort. Null reference. These aren't "expected failures"—they're catastrophic or indicate bugs. You want them to crash loudly with a stack trace, not be wrapped in a Result that someone might accidentally ignore.

### .NET Ecosystem Integration

The BCL throws. Entity Framework throws. HttpClient throws. ASP.NET middleware expects exceptions. Fighting this means conversion code at every boundary:

```csharp
// This gets tedious across an entire codebase
var result = Result.Try(() => _dbContext.SaveChangesAsync());
```

If your application is mostly gluing together .NET libraries, Results create friction for marginal benefit.

### Rare Failures

If 99.9% of calls succeed, exceptions are essentially free (no overhead until thrown). Results allocate an object and require an IsSuccess check on every call—small overhead, but real.

### Prototyping and Exploration

When you're figuring out what errors are even possible, exceptions let you focus on the happy path. Failures crash with useful stack traces. Forcing Result handling before you understand the failure modes slows exploration.

---

## Where Results Excel

### Expected Business Failures

"Insufficient inventory" isn't exceptional—it's a normal business outcome. Treating it the same as "database server caught fire" conflates different categories of failure. Results let business outcomes be return values, not exceptions.

### Shallow, Controlled Architectures

In a 3-4 layer architecture where you control all the code, Result propagation isn't much overhead, and the explicitness is valuable. You can read the code linearly and understand error flow.

### Complex Error Information

When errors need structured data—codes, context dictionaries, multiple validation failures—Result types model this naturally. Exception properties exist but feel bolted-on.

```csharp
// Result with structured context
new Error("Insufficient inventory", ErrorCodes.InsufficientInventory)
    .WithContext("ItemId", 42)
    .WithContext("Requested", 100)
    .WithContext("Available", 7);

// Exception equivalent is clunkier
throw new InventoryException("Insufficient inventory") 
{ 
    ItemId = 42, 
    Requested = 100, 
    Available = 7 
};
```

### Partial Failure Scenarios

When processing a batch and items 1-3 succeed but item 4 fails, Results make the partial state explicit. With exceptions, you need careful state tracking before the throw.

### Functional Composition

If you're building pipelines of operations, Bind/Map chains compose cleanly:

```csharp
GetUser(id)
    .Bind(user => ValidatePermissions(user))
    .Bind(user => GetOrders(user.Id))
    .Map(orders => orders.Where(o => o.Status == Active));
```

The equivalent with exceptions is nested try-catch blocks.

### API Boundaries Where Callers Must Handle Failure

If ignoring an error is always wrong, Results force acknowledgment. You can ignore an exception (forget the catch), but you have to actively discard a Result.

---

## The Honest Comparison

| Factor | Favors Exceptions | Favors Results |
|--------|-------------------|----------------|
| Failure frequency | Rare (<1%) | Common (>5%) |
| Call stack depth | Deep (10+ layers) | Shallow (2-4 layers) |
| Ecosystem | Heavy .NET/BCL use | Greenfield, controlled code |
| Error information | Simple message + trace | Structured codes + context |
| Team background | Typical C#/Java | F#/Rust/functional |
| Failure type | Bugs, infrastructure | Business rules, validation |
| Code style | OOP, imperative | Functional composition |

---

## What the Industry Does

**C# / .NET**: Exceptions are dominant. The framework, libraries, and community all expect them. Result patterns are growing via libraries like FluentResults and ErrorOr, but remain a minority choice.

**Java**: Similar to C#. Checked exceptions were an experiment in forced handling, widely considered unsuccessful due to boilerplate. Most code uses unchecked exceptions.

**Go**: Error returns everywhere. `value, err := thing()` is the pattern. No exceptions exist. The community is divided—some love the explicitness, others find it verbose.

**Rust**: Results everywhere, and it's considered a success. The `?` operator makes propagation ergonomic. Rust developers generally prefer this to exceptions.

**Kotlin / Swift**: Hybrid. Both have exceptions AND Result types. The community uses both depending on context.

**TypeScript / JavaScript**: Exceptions exist, but Promises introduced `.then()/.catch()` which is Result-adjacent. The community increasingly uses Result-like patterns for business logic.

---

## Thought Leaders

**Scott Wlaschin** (F# for Fun and Profit): Strong Result advocate. His "Railway Oriented Programming" talks have influenced many C# developers. Coming from F# where Results are idiomatic.

**Eric Lippert** (former C# compiler team): Distinguishes "boneheaded" exceptions (bugs—should crash) from "vexing" exceptions (expected failures—should use TryParse patterns). The latter is Result-adjacent.

**Joe Duffy** (Microsoft Midori OS): Led an experimental OS in systems C#. They abandoned exceptions entirely for Result-like error codes, finding that exceptions' hidden control flow made reasoning about program state too difficult. His retrospective is influential.

**Rob Pike** (Go team): Explicitly rejected exceptions for Go. Argues error returns make handling visible, even if verbose.

**Joel Spolsky**: Famously called exceptions "invisible gotos." But also acknowledged that error-code-heavy code becomes noise that obscures business logic—the problem exceptions were designed to solve.

---

## Practical Recommendations

### For Most C# Line-of-Business Applications

**Use exceptions.** The ecosystem expects them, teams know them, and the benefits of Results don't outweigh the friction of fighting .NET conventions.

Adopt exception best practices:
- Catch at boundaries (controllers, handlers)
- Don't catch and swallow
- Use specific exception types
- Preserve stack traces (`throw;` not `throw ex;`)

### For New Greenfield Code with Functional Leanings

**Consider Results** for your domain/business logic, especially if:
- Failures are common and expected (validation, business rules)
- You want structured error information
- Your team has functional programming exposure

But still:
- Convert to exceptions at public API boundaries if consumers expect that
- Wrap external exception-throwing code in Results at infrastructure boundaries

### For Mixed Codebases

**Hybrid approach:**
- Results for internal domain logic where failures are expected
- Exceptions for truly exceptional cases and .NET interop
- Clear boundaries where conversion happens

---

## The Real Mistake

Dogma in either direction. Insisting on Results everywhere in a .NET codebase creates constant friction. Insisting on exceptions everywhere ignores legitimate benefits of explicit error handling for business logic.

Understand both patterns. Apply them where they fit.