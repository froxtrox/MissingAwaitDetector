# MAWT009: Task inspected before return

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT009               |
| Category    | Usage                  |
| Severity    | Warning                |
| Enabled     | True                   |

## Description

A `Task` or `Task<T>` is assigned to a local variable, used or inspected (e.g., by
accessing a property or passing it to another method), and then returned from the current
method. This pattern is often unnecessary and can introduce subtle bugs if the inspection
triggers synchronous evaluation or inadvertently observes an incomplete state.

If the method does not need to inspect the task, it can return the task directly. If
inspection is truly needed, consider making the method `async` and awaiting the task so
that error handling and control flow behave predictably.

## Violation

```csharp
Task<int> GetCountAsync()
{
    Task<int> task = ComputeCountAsync();

    // MAWT009: task is inspected before being returned
    if (task.IsFaulted)
    {
        Log("Count computation failed");
    }

    return task;
}
```

## Fix

Either return the task directly or make the method async.

```csharp
// Option 1: Return directly (if no inspection is needed)
Task<int> GetCountAsync()
{
    return ComputeCountAsync();
}

// Option 2: Make the method async (if inspection is needed)
async Task<int> GetCountAsync()
{
    try
    {
        return await ComputeCountAsync();
    }
    catch (Exception ex)
    {
        Log("Count computation failed");
        throw;
    }
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT009.severity = suggestion
```
