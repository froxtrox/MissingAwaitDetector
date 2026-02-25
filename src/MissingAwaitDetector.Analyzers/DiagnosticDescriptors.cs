using Microsoft.CodeAnalysis;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// Central registry of all MissingAwaitDetector diagnostic descriptors.
    /// </summary> 
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Async";
        private const string HelpLinkBase = "https://github.com/froxtrox/MissingAwaitDetector/blob/main/docs/rules/";

        public static readonly DiagnosticDescriptor TaskTreatedAsValue = new(
            id: DiagnosticIds.TaskTreatedAsValue,
            title: "Task treated as value",
            messageFormat: "Task{0} is being treated as if it were {1}. Tasks represent future values, not current values. Use 'await' to get the actual value.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A Task or Task<T> is being used in a context where its underlying type is expected. Tasks are not values - they are promises of future values. Use 'await' to unwrap them.",
            helpLinkUri: HelpLinkBase + "MAWT001.md");

        public static readonly DiagnosticDescriptor SynchronousUnwrap = new(
            id: DiagnosticIds.SynchronousUnwrap,
            title: "Task synchronously unwrapped",
            messageFormat: "Task is being unwrapped synchronously via '.{0}'. This blocks the calling thread and can cause deadlocks. Use 'await' instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Synchronously unwrapping a Task via .Result, .Wait(), or .GetAwaiter().GetResult() blocks the thread and risks deadlocks, especially in ASP.NET contexts.",
            helpLinkUri: HelpLinkBase + "MAWT002.md");

        public static readonly DiagnosticDescriptor TaskStateInspection = new(
            id: DiagnosticIds.TaskStateInspection,
            title: "Task state inspection",
            messageFormat: "Task state is being inspected via '.{0}'. Prefer awaiting the Task instead of polling its state.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Inspecting Task.Status, Task.IsCompleted, Task.IsFaulted, Task.Id, Task.AsyncState and similar properties is rarely correct in application code. Await the Task to observe its result, or use dedicated application-level abstractions (e.g. Activity.Id) instead of Task infrastructure properties.",
            helpLinkUri: HelpLinkBase + "MAWT003.md");

        public static readonly DiagnosticDescriptor FireAndForget = new(
            id: DiagnosticIds.FireAndForget,
            title: "Fire-and-forget Task",
            messageFormat: "Task-returning method '{0}' is called but the result is not awaited or stored. Unhandled exceptions will crash the application.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Calling a Task-returning method without awaiting or storing the result means exceptions go unobserved and the operation has no backpressure.",
            helpLinkUri: HelpLinkBase + "MAWT004.md");

        public static readonly DiagnosticDescriptor StoredNeverAwaited = new(
            id: DiagnosticIds.StoredNeverAwaited,
            title: "Task stored but never awaited",
            messageFormat: "Task variable '{0}' is created but never awaited or composed. Either await it or remove the unnecessary variable.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A Task is stored in a variable but never awaited, returned, or composed with Task.WhenAll/WhenAny. This is likely a bug.",
            helpLinkUri: HelpLinkBase + "MAWT005.md");

        public static readonly DiagnosticDescriptor ValueTaskMisuse = new(
            id: DiagnosticIds.ValueTaskMisuse,
            title: "ValueTask misuse",
            messageFormat: "ValueTask is being misused: {0}. ValueTask must be awaited exactly once, immediately after creation.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "ValueTask must be awaited exactly once and immediately. It cannot be stored, awaited multiple times, or used after other asynchronous operations.",
            helpLinkUri: HelpLinkBase + "MAWT006.md");

        public static readonly DiagnosticDescriptor LinqTaskCollection = new(
            id: DiagnosticIds.LinqTaskCollection,
            title: "LINQ produces unawaited Task collection",
            messageFormat: "LINQ expression produces a collection of Tasks that are not being awaited. Use 'await Task.WhenAll(...)' to execute them.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A LINQ expression produces IEnumerable<Task> or similar but the Tasks are not composed with Task.WhenAll/WhenAny. This creates a collection of unexecuted promises.",
            helpLinkUri: HelpLinkBase + "MAWT007.md");

        public static readonly DiagnosticDescriptor SyncUnwrapInAsyncMethod = new(
            id: DiagnosticIds.SyncUnwrapInAsyncMethod,
            title: "Synchronous unwrap inside async method",
            messageFormat: "Synchronous Task unwrap via '.{0}' inside an async method. You are already in an async context - use 'await' instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Using .Result, .Wait(), or .GetAwaiter().GetResult() inside an async method is almost certainly a mistake. You are already in an async context and should use 'await'.",
            helpLinkUri: HelpLinkBase + "MAWT008.md");

        public static readonly DiagnosticDescriptor TaskUsedBeforeReturn = new(
            id: DiagnosticIds.TaskUsedBeforeReturn,
            title: "Task inspected before return",
            messageFormat: "Task variable '{0}' is inspected/used before being returned. Consider returning the Task directly or making the method async.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A Task is stored, used or inspected, then returned. This is usually a code smell - either return the Task directly (pass-through) or make the method async.",
            helpLinkUri: HelpLinkBase + "MAWT009.md");

        public static readonly DiagnosticDescriptor AsyncVoidMethod = new(
            id: DiagnosticIds.AsyncVoidMethod,
            title: "Async void method",
            messageFormat: "Method '{0}' is 'async void'. Exceptions cannot be caught by the caller and will crash the process. Use 'async Task' instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Async void methods are dangerous because unhandled exceptions will terminate the process. The only valid use of async void is for event handlers. Use 'async Task' to allow callers to await and observe exceptions.",
            helpLinkUri: HelpLinkBase + "MAWT010.md");
    }
}
