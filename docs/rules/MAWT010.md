# MAWT010: Async void method

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT010               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A method is declared `async void`. Async void methods are dangerous because any unhandled
exception will crash the process — the caller has no way to catch or observe it. The only
valid use of `async void` is for event handlers that must match a `void` delegate signature.

### Suppressions

This diagnostic is **not** reported when:

- The method matches the event handler signature `(object sender, EventArgs e)` where
  the second parameter derives from `System.EventArgs`.

## Violation

```csharp
// MAWT010: async void — exceptions will crash the process
async void DoWorkAsync()
{
    await Task.Delay(100);
    throw new InvalidOperationException("oops");
}

// MAWT010: async void local function
void M()
{
    async void LocalAsync() => await Task.Delay(1);
}
```

## Fix

Change the return type to `Task`.

```csharp
// Fixed: caller can now await and observe exceptions
async Task DoWorkAsync()
{
    await Task.Delay(100);
    throw new InvalidOperationException("oops");
}

// Event handler — async void is acceptable here
async void OnButtonClick(object sender, EventArgs e)
{
    await SaveAsync();
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT010.severity = error
```
