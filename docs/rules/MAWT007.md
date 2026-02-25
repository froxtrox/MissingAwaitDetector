# MAWT007: LINQ produces unawaited Task collection

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT007               |
| Category    | Usage                  |
| Severity    | Warning                |
| Enabled     | True                   |

## Description

A LINQ projection (such as `Select`) is being used with an async delegate, producing an
`IEnumerable<Task>` or `IEnumerable<Task<T>>`. The resulting collection contains tasks
that have been started but are never awaited. Exceptions thrown by any of these tasks
will go unobserved, and the caller has no guarantee that the work has completed.

Detected patterns include:

- `collection.Select(x => AsyncMethod(x))`
- `collection.Select(async x => await AsyncMethod(x))`
- Similar projections producing `Task` collections without `Task.WhenAll`

## Violation

```csharp
async Task Example(List<string> urls)
{
    // MAWT007: Select produces IEnumerable<Task<string>> that is never awaited
    var tasks = urls.Select(url => DownloadAsync(url));

    // MAWT007: Even .ToList() only materializes tasks, does not await them
    var taskList = urls.Select(url => DownloadAsync(url)).ToList();
}
```

## Fix

Use `Task.WhenAll` to await all tasks in the collection.

```csharp
async Task Example(List<string> urls)
{
    // Option 1: WhenAll with Select
    string[] results = await Task.WhenAll(urls.Select(url => DownloadAsync(url)));

    // Option 2: Explicit loop
    foreach (var url in urls)
    {
        await DownloadAsync(url);
    }
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT007.severity = warning
```
