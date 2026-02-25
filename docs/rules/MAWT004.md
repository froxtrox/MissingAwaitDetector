# MAWT004: Fire-and-forget Task

| Property    | Value                  |
|-------------|------------------------|
| Rule ID     | MAWT004               |
| Category    | Usage                  |
| Severity    | Error                  |
| Enabled     | True                   |

## Description

A Task-returning method is called but its return value is neither awaited, stored in a
variable, nor returned. This creates a fire-and-forget call where exceptions are silently
swallowed and the caller has no way to observe completion or failure.

### Suppressions

This diagnostic is suppressed when:

- The method is annotated with `[FireAndForget]`
- The result is explicitly discarded with `_ = SomeAsync()`

Using the discard pattern signals intentional fire-and-forget to both the analyzer and
future readers of the code.

## Violation

```csharp
async Task Example()
{
    // MAWT004: Task returned by SendEmailAsync is ignored
    SendEmailAsync("user@example.com", "Hello");

    // MAWT004: Task returned by LogAsync is ignored
    LogAsync("operation started");
}
```

## Fix

Await the task, or use an explicit discard if fire-and-forget is intentional.

```csharp
async Task Example()
{
    // Option 1: Await the call
    await SendEmailAsync("user@example.com", "Hello");

    // Option 2: Explicit discard (intentional fire-and-forget)
    _ = LogAsync("operation started");
}
```

## Configuration

Override severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MAWT004.severity = error
```
