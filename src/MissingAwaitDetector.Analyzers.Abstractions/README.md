# MissingAwaitDetector.Analyzers.Abstractions

Companion attributes for the [MissingAwaitDetector.Analyzers](https://www.nuget.org/packages/MissingAwaitDetector.Analyzers) Roslyn analyzer.

## Installation

```bash
dotnet add package MissingAwaitDetector.Analyzers.Abstractions
```

> **Note:** Most users only need the main analyzer package. Install this package only if you want self-documenting suppression attributes instead of using `_ =` discards or `.editorconfig` configuration.

## Attributes

### `[FireAndForget]`

Suppresses **MAWT004** (discarded Task) and **MAWT005** (unawaited stored Task) for methods you intentionally call without awaiting.

```csharp
using MissingAwaitDetector;

[FireAndForget("Background telemetry - exceptions are logged and swallowed")]
public async Task UploadTelemetryAsync() { /* ... */ }
```

### `[AllowSynchronousIO]`

Suppresses **MAWT002** and **MAWT008** (synchronous blocking on async) for methods where blocking is intentional.

```csharp
using MissingAwaitDetector;

[AllowSynchronousIO("COM interop requirement")]
public int LegacyBridge()
{
    return GetDataAsync().GetAwaiter().GetResult();
}
```

## Alternative Suppression Methods

If you don't need attribute-based suppressions, these approaches work without this package:

- **Explicit discard:** `_ = ProcessAsync();`
- **`.editorconfig`:** `dotnet_diagnostic.MAWT004.severity = none`

## License

MIT - see [LICENSE](https://github.com/froxtrox/MissingAwaitDetector/blob/main/LICENSE)
