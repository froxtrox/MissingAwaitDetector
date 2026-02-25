namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// All diagnostic IDs for the MissingAwaitDetector analyzer suite.
    /// </summary>
    internal static class DiagnosticIds
    {
        /// <summary>Task treated as value (ToString, interpolation, passed where T expected).</summary>
        public const string TaskTreatedAsValue = "MAWT001";

        /// <summary>Task synchronously unwrapped (.Result, .Wait(), .GetAwaiter().GetResult()).</summary>
        public const string SynchronousUnwrap = "MAWT002";

        /// <summary>Task state inspection (.Status, .IsCompleted, .IsFaulted, etc.).</summary>
        public const string TaskStateInspection = "MAWT003";

        /// <summary>Fire-and-forget: Task-returning method called but result discarded.</summary>
        public const string FireAndForget = "MAWT004";

        /// <summary>Task stored in variable but never awaited or composed.</summary>
        public const string StoredNeverAwaited = "MAWT005";

        /// <summary>ValueTask misuse (multiple await, delayed await, stored in collection).</summary>
        public const string ValueTaskMisuse = "MAWT006";

        /// <summary>LINQ produces IEnumerable&lt;Task&gt; but Tasks are not composed.</summary>
        public const string LinqTaskCollection = "MAWT007";

        /// <summary>Synchronous unwrap inside async method (already in async context).</summary>
        public const string SyncUnwrapInAsyncMethod = "MAWT008";

        /// <summary>Task inspected/used before being returned (code smell).</summary>
        public const string TaskUsedBeforeReturn = "MAWT009";

        /// <summary>Async void method (exceptions cannot be caught by the caller).</summary>
        public const string AsyncVoidMethod = "MAWT010";
    }
}
