# MAWT006: ValueTask misuse

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT006               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A `ValueTask` or `ValueTask<T>` is being used incorrectly. Unlike `Task`, a `ValueTask`
must be awaited exactly once, and it must be consumed promptly. Violating these rules
leads to undefined behavior because the underlying `IValueTaskSource` may be recycled.

Detected patterns include:

- Awaiting the same `ValueTask` variable more than once
- Storing a `ValueTask` in a field or collection for later use
- Awaiting a `ValueTask` after other asynchronous operations (delayed consumption)
- Calling `.Result` or `.GetAwaiter().GetResult()` on a non-completed `ValueTask`

## Violation

```csharp
async Task Example()
{
    ValueTask<int> vt = GetValueAsync();

    // MAWT006: ValueTask awaited after an intervening await
    await Task.Delay(100);
    int first = await vt;

    // MAWT006: ValueTask awaited a second time
    int second = await vt;
}
```

## Fix

Await the `ValueTask` immediately. If you need to store or reuse the result, convert it
to a `Task` first with `.AsTask()`.

```csharp
async Task Example()
{
    // Option 1: Await immediately
    int value = await GetValueAsync();

    // Option 2: Convert to Task if storage is needed
    Task<int> task = GetValueAsync().AsTask();
    await Task.Delay(100);
    int first = await task;
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT006.severity = error
```
