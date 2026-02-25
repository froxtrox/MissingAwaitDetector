# MAWT001: Task treated as value

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT001               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A `Task` or `Task<T>` is being used as if it were its result value. This typically
means the task was never awaited, and the code is operating on the Task object itself
rather than the value it produces.

Detected patterns include:

- Calling `ToString()` on a Task (produces `"System.Threading.Tasks.Task"`)
- Calling `GetHashCode()` or `Equals()` on a Task
- Using a Task inside string interpolation (`$"Result: {someTask}"`)
- Passing a `Task<T>` to a parameter that expects `T`

## Violation

```csharp
async Task Example()
{
    Task<int> countTask = GetCountAsync();

    // MAWT001: Task<int> used in string interpolation
    Console.WriteLine($"Count is: {countTask}");

    // MAWT001: ToString() called on Task
    string text = countTask.ToString();

    // MAWT001: Task<int> passed where int is expected
    SetValue(countTask);
}
```

## Fix

Await the task to obtain the underlying value before using it.

```csharp
async Task Example()
{
    int count = await GetCountAsync();

    Console.WriteLine($"Count is: {count}");
    string text = count.ToString();
    SetValue(count);
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT001.severity = error
```
