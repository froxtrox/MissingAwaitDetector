# MAWT003: Task state inspection

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT003               |
| Category    | Usage                  |
| Severity    | Warning                |
| Enabled     | True                   |

## Description

A `Task` is being inspected via a property that should not appear in application code.
This covers two related patterns:

**State-inspection properties** — polling a task's completion state instead of awaiting it.
Checking `.IsCompleted`, `.Status`, etc. is fragile and usually indicates a missing `await`.
The proper way to observe a task's outcome is to await it and handle exceptions with try/catch.

**Diagnostic/metadata properties** — accessing infrastructure properties that exist solely
for debuggers, profilers, and ETW traces. `Task.Id`, `Task.CurrentId`, `Task.AsyncState`,
and `Task.CreationOptions` carry no meaningful semantic for business logic.

Detected property accesses:

- `.Status`
- `.IsCompleted`
- `.IsCompletedSuccessfully`
- `.IsFaulted`
- `.IsCanceled`
- `.Exception`
- `.Id`              — diagnostic-only; use `Guid` / `Activity.Id` for correlation
- `.CurrentId`       — static; ETW/debugger infrastructure only
- `.AsyncState`      — legacy API; use closure captures instead
- `.CreationOptions` — scheduler configuration; never branch on this in app logic

## Property-Level Configuration

Exclude specific Task properties from MAWT003 using `.editorconfig`. This is useful when
your team has a legitimate need for certain properties (e.g. `Task.Id` inside a diagnostics
helper, or `Task.AsyncState` in a legacy bridge layer) without silencing the entire rule.

```ini
[*.cs]
# Comma-separated list of Task property names to skip (case-sensitive).
dotnet_diagnostic.MAWT003.excluded_task_properties = Id, CurrentId

# To suppress only the diagnostic/metadata properties and keep state-inspection checks:
# dotnet_diagnostic.MAWT003.excluded_task_properties = Id, CurrentId, AsyncState, CreationOptions
```

Unknown property names are silently ignored. When the key is absent, all 10 properties are
flagged (default behaviour).

## Violation

```csharp
async Task Example()
{
    Task<int> task = ComputeAsync();

    // MAWT003: Polling task state instead of awaiting
    if (task.IsCompleted)
    {
        Console.WriteLine("Done");
    }

    // MAWT003: Task.Id is a debug identifier, not a correlation ID
    logger.Log($"Task {task.Id} started");

    // MAWT003: AsyncState is a legacy pre-lambda API
    var ctx = (RequestContext)task.AsyncState;
}
```

## Fix

Await the task for state observation; use application-level abstractions for correlation.

```csharp
async Task Example()
{
    // Await instead of polling state
    try
    {
        int result = await ComputeAsync();
        Console.WriteLine("Done");
    }
    catch (Exception ex)
    {
        Log(ex);
    }
}

// Use a Guid or Activity.Id for correlation — not Task.Id
var correlationId = Guid.NewGuid();
logger.Log($"Operation {correlationId} started");

// Use closure capture instead of AsyncState
var ctx = requestContext; // captured by closure
var task = Task.Run(() => Process(ctx));
```

## Severity Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT003.severity = warning
```
