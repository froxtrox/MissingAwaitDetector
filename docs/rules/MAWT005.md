# MAWT005: Task stored but never awaited

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT005               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A `Task` or `Task<T>` is assigned to a local variable but is never awaited, returned,
or composed (e.g., passed to `Task.WhenAll` or `Task.WhenAny`). The task runs in the
background and any exceptions it throws will go unobserved. This is almost always a bug
where an `await` was accidentally omitted.

The analyzer tracks the variable through the method and reports if none of the following
occur before the variable goes out of scope:

- The variable is awaited (`await task`)
- The variable is returned (`return task`)
- The variable is passed to a composition method (`Task.WhenAll(task)`)

## Violation

```csharp
async Task Example()
{
    // MAWT005: task is stored but never awaited
    Task<string> task = FetchDataAsync();

    Console.WriteLine("Doing other work...");

    // Method exits without awaiting, returning, or composing task
}
```

## Fix

Await the stored task before the method completes.

```csharp
async Task Example()
{
    Task<string> task = FetchDataAsync();

    Console.WriteLine("Doing other work...");

    string data = await task;
    Console.WriteLine(data);
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT005.severity = error
```
