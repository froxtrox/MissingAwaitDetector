### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MAWT001 | Async | Error | Task treated as value
MAWT002 | Async | Error | Task synchronously unwrapped
MAWT003 | Async | Warning | Task state inspection
MAWT004 | Async | Error | Fire-and-forget Task
MAWT005 | Async | Error | Task stored but never awaited
MAWT006 | Async | Error | ValueTask misuse
MAWT007 | Async | Warning | LINQ produces unawaited Task collection
MAWT008 | Async | Error | Synchronous unwrap inside async method
MAWT009 | Async | Warning | Task inspected before return
MAWT010 | Async | Error | Async void method
