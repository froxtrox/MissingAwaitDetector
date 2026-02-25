# MAWT008: Synchronous unwrap inside async method

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT008               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A `Task` is being synchronously unwrapped with `.Result`, `.Wait()`, or
`.GetAwaiter().GetResult()` inside a method that is already marked `async`. Because the
method is async, there is no reason to block -- `await` should be used instead.

Blocking inside an async method is especially dangerous because:

- It can deadlock on single-threaded synchronization contexts (UI, legacy ASP.NET)
- It wastes the thread pool thread that the async state machine is running on
- It defeats the purpose of the method being async in the first place

This rule differs from MAWT002 in that it specifically targets async methods, where a
direct `await` replacement is always available.

## Violation

```csharp
async Task<string> FetchAndFormatAsync()
{
    // MAWT008: .Result used inside async method
    var data = GetDataAsync().Result;

    // MAWT008: .Wait() used inside async method
    SaveDataAsync(data).Wait();

    return data;
}
```

## Fix

Replace the synchronous unwrap with `await`.

```csharp
async Task<string> FetchAndFormatAsync()
{
    var data = await GetDataAsync();
    await SaveDataAsync(data);
    return data;
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT008.severity = error
```
