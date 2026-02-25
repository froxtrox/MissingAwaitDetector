using System;

namespace MissingAwaitDetector
{
    /// <summary>
    /// Marks a method as intentionally using synchronous Task unwrapping.
    /// Suppresses MAWT002 and MAWT008 diagnostics within the method body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AllowSynchronousIOAttribute : Attribute
    {
        /// <summary>
        /// Gets the justification for why synchronous IO is allowed.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AllowSynchronousIOAttribute"/>.
        /// </summary>
        /// <param name="reason">Optional justification for the synchronous IO usage.</param>
        public AllowSynchronousIOAttribute(string? reason = null)
        {
            Reason = reason;
        }
    }
}
