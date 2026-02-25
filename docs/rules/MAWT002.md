# MAWT002: Task synchronously unwrapped

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT002               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A `Task` or `Task<T>` is being synchronously unwrapped using `.Result`, `.Wait()`, or
`.GetAwaiter().GetResult()`. These calls block the current thread and can cause deadlocks
in UI or ASP.NET synchronization contexts.

Detected patterns include:

- Accessing `.Result` on a Task
- Calling `.Wait()` on a Task
- Calling `.GetAwaiter().GetResult()` on a Task

### Suppressions

This diagnostic is suppressed in the following cases:

- Inside a `Main()` entry point method (where async may not be available)
- When the method or call site is annotated with `[AllowSynchronousIO]`

## Violation

```csharp
void ProcessData()
{
    // MAWT002: Synchronous unwrap via .Result
    var data = FetchDataAsync().Result;

    // MAWT002: Synchronous unwrap via .Wait()
    SaveDataAsync(data).Wait();

    // MAWT002: Synchronous unwrap via .GetAwaiter().GetResult()
    var count = GetCountAsync().GetAwaiter().GetResult();
}
```

## Fix

Make the method async and use `await` instead of blocking calls.

```csharp
async Task ProcessDataAsync()
{
    var data = await FetchDataAsync();
    await SaveDataAsync(data);
    var count = await GetCountAsync();
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT002.severity = error
```
