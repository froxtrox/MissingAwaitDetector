using System;

namespace MissingAwaitDetector
{
    /// <summary>
    /// Marks a Task-returning method as intentionally fire-and-forget.
    /// Suppresses MAWT004 (fire-and-forget) diagnostics for callers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class FireAndForgetAttribute : Attribute
    {
        /// <summary>
        /// Gets the justification for why this method is fire-and-forget.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="FireAndForgetAttribute"/>.
        /// </summary>
        /// <param name="reason">Optional justification for the fire-and-forget usage.</param>
        public FireAndForgetAttribute(string? reason = null)
        {
            Reason = reason;
        }
    }
}
