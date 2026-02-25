# MissingAwaitDetector

[![NuGet](https://img.shields.io/nuget/v/MissingAwaitDetector.Analyzers.svg)](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MissingAwaitDetector.Analyzers.svg)](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers)

**Your code compiles. Your code runs. Your code is wrong.**

Ever spent hours debugging why your API returns `System.Threading.Tasks.Task1[UserDto]` instead of actual user data? Or worse — you passed a `Task<int>` where an `int` was expected, and now you're staring at a Task ID wondering why your "order total" is `42917381`?

These bugs don't throw. They don't crash. They just silently do the wrong thing. And they cost real time — time you could've spent building something instead of questioning your sanity.

## Why This Exists

In an ideal world, you'd never need this package. Your codebase would have perfect async discipline from day one. But we don't live in that world — we live in the world of tight deadlines, legacy codebases, and that one PR where someone forgot an `await` and it somehow passed code review.

MissingAwaitDetector is a Roslyn analyzer that catches async/await mistakes **at compile time** — the kind that are structurally valid C# but semantically dangerous. It's not about style preferences. It's about catching real bugs before they reach production.

Whether you're:

- **A new developer** navigating async/await for the first time — the [sample project](samples/MissingAwaitDetector.Analyzers.Sample) walks through every rule with clear bad/good examples to help you build intuition around async patterns in C#
- **A seasoned developer** migrating a codebase from synchronous to async APIs (hello, Redis `IDatabase` to `IDatabaseAsync`) and want a safety net that catches every missed `await` across hundreds of call sites

This package has your back.

## Quick Start

```bash
dotnet add package MissingAwaitDetector.Analyzers
```

That's it. Build your project and the analyzers light up immediately.

| Package | NuGet |
|---------|-------|
| [MissingAwaitDetector.Analyzers](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers) | [![NuGet](https://img.shields.io/nuget/v/MissingAwaitDetector.Analyzers.svg)](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers) |
| [MissingAwaitDetector.Analyzers.Abstractions](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers.Abstractions) | [![NuGet](https://img.shields.io/nuget/v/MissingAwaitDetector.Analyzers.Abstractions.svg)](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers.Abstractions) |

> The **Analyzers** package is all you need. The **Abstractions** package is optional — install it only if you want the `[FireAndForget]` and `[AllowSynchronousIO]` suppression attributes.

## What It Catches

| ID | Default | What went wrong |
|------|---------|-----------------|
| [MAWT001](docs/rules/MAWT001.md) | Error | You used a `Task` like it was the actual value — string interpolation, `ToString()`, passing `Task<T>` where `T` is expected |
| [MAWT002](docs/rules/MAWT002.md) | Error | You called `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` — blocking the thread and risking deadlocks |
| [MAWT003](docs/rules/MAWT003.md) | Warning | You're checking `.IsCompleted`, `.Status`, `.IsFaulted` — polling Task state instead of just awaiting it |
| [MAWT004](docs/rules/MAWT004.md) | Error | You called an async method and threw away the Task — unhandled exceptions will crash your app |
| [MAWT005](docs/rules/MAWT005.md) | Error | You stored a Task in a variable and never awaited, returned, or composed it |
| [MAWT006](docs/rules/MAWT006.md) | Error | You misused a `ValueTask` — awaited it twice or held onto it too long |
| [MAWT007](docs/rules/MAWT007.md) | Warning | Your LINQ `.Select()` produced a bunch of Tasks that nobody awaited — you wanted `await Task.WhenAll(...)` |
| [MAWT008](docs/rules/MAWT008.md) | Error | You called `.Result` or `.Wait()` **inside an async method** — you're already async, just use `await` |
| [MAWT009](docs/rules/MAWT009.md) | Warning | You stored a Task, poked at it, then returned it — either return it directly or make the method `async` |
| [MAWT010](docs/rules/MAWT010.md) | Error | You used `async void` — exceptions will crash the process. Use `async Task` (event handlers are exempt) |

## Real Examples

These are the kind of bugs that pass code review and waste your afternoon:

```csharp
// MAWT001 — Looks right, isn't right
var user = GetUserAsync();           // user is Task<UserDto>, not UserDto
return Ok(user);                     // Your API returns a serialized Task object

// MAWT004 — Fire and forget (and crash later)
SaveAuditLogAsync(record);           // Nobody awaited this. If it throws, your app dies.

// MAWT005 — Stored but forgotten
var warmup = CacheWarmupAsync();     // You meant to await this later...
DoOtherStuff();                      // ...but you never did.

// MAWT002 — "I'll just use .Result, what's the worst that can happen?"
var data = GetDataAsync().Result;    // Deadlock on ASP.NET. Have fun.

// MAWT010 — async void is a ticking time bomb
async void OnButtonClick()           // If this throws, your process dies.
{                                    // Use async Task instead.
    await DoWorkAsync();
}
```

## Code Fixes

The analyzer doesn't just complain — it offers one-click fixes via the lightbulb (Ctrl+.) in your IDE.

### MAWT001, MAWT002, MAWT005, MAWT008 — "Add `await` and make method async"

The fixer adds `await`, marks the method `async`, and upgrades the return type (`void` → `Task`, `T` → `Task<T>`). It's smart about the specific pattern:

```csharp
// .Result → await
var data = GetDataAsync().Result;        // BEFORE
var data = await GetDataAsync();         // AFTER

// .Wait() → await
task.Wait();                             // BEFORE
await task;                              // AFTER

// .GetAwaiter().GetResult() → await
var r = GetAsync().GetAwaiter()          // BEFORE
    .GetResult();
var r = await GetAsync();                // AFTER

// Stored but never awaited → await at assignment
var task = GetDataAsync();               // BEFORE (MAWT005)
var task = await GetDataAsync();         // AFTER
```

If the method is already `async`, it won't add the keyword again — it just replaces the blocking call with `await`.

### MAWT004 — Fire-and-forget (two options)

**Option 1: Add `await`** — adds `await` and makes the method async:
```csharp
void M() { ProcessAsync(); }            // BEFORE
async Task M() { await ProcessAsync(); } // AFTER
```

**Option 2: Discard with `_ =`** — acknowledges the fire-and-forget without changing the method signature:
```csharp
void M() { ProcessAsync(); }            // BEFORE
void M() { _ = ProcessAsync(); }        // AFTER
```

## Suppression Attributes

Sometimes you genuinely mean it. For those cases, install the companion package:

```bash
dotnet add package MissingAwaitDetector.Analyzers.Abstractions
```

```csharp
using MissingAwaitDetector;

// "Yes, I know this is fire-and-forget. I've handled errors internally."
[FireAndForget("Background telemetry — exceptions are logged and swallowed")]
public async Task UploadTelemetryAsync() { /* ... */ }

// "Yes, I know this blocks. It's a legacy sync interface I can't change yet."
[AllowSynchronousIO("COM interop requirement")]
public int LegacyBridge()
{
    return GetDataAsync().GetAwaiter().GetResult();
}
```

You can also use explicit discard to acknowledge fire-and-forget:
```csharp
_ = ProcessAsync();  // "I see you, Task. I'm choosing to ignore you."
```

## A Note on Severity Defaults

All rules ship **enabled**, and most default to **Error**. This is intentional — these are the kind of bugs that silently corrupt data or crash apps at 2 AM. I'd rather you see them all upfront and choose which ones to dial back, than have them hiding as suggestions you never notice.

If a rule is too noisy for your codebase, configure it in your `.editorconfig`:

```ini
[*.cs]
# Downgrade to warning
dotnet_diagnostic.MAWT003.severity = warning

# Turn off entirely
dotnet_diagnostic.MAWT009.severity = none

# MAWT003: exclude specific Task properties from inspection warnings
dotnet_diagnostic.MAWT003.excluded_task_properties = Id, CurrentId
```

See the [.editorconfig docs](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options) for the full configuration reference.

## Patterns the Analyzer Recognizes as Safe

The analyzer isn't trying to fight you. These patterns won't trigger diagnostics:

```csharp
await task;                           // Awaited — the right thing to do
await task.ConfigureAwait(false);     // ConfigureAwait is fine
Task.WhenAll(task1, task2);           // Composition
Task.WhenAny(task1, task2);           // Racing
return task;                          // Pass-through return
tasks.Add(task);                      // Collecting for later WhenAll
_ = ProcessAsync();                   // Explicit discard — you know what you're doing
async void OnClick(object s, EventArgs e) { } // Event handlers are exempt from MAWT010
```

## Contributing

Found a bug? Have a suggestion? [Open an issue](https://github.com/froxtrox/MissingAwaitDetector/issues).

## License

MIT
