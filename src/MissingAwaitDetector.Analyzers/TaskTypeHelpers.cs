using System.Threading;
using Microsoft.CodeAnalysis;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// Helpers for identifying Task, Task&lt;T&gt;, ValueTask, and ValueTask&lt;T&gt; types.
    /// </summary>
    internal static class TaskTypeHelpers
    {
        /// <summary>
        /// Returns true if the type is Task, Task&lt;T&gt;, ValueTask, or ValueTask&lt;T&gt;.
        /// </summary>
        public static bool IsTaskLike(ITypeSymbol? type)
        {
            return IsTask(type) || IsValueTask(type);
        }

        /// <summary>
        /// Returns true if the type is System.Threading.Tasks.Task or Task&lt;T&gt;.
        /// </summary>
        public static bool IsTask(ITypeSymbol? type)
        {
            if (type is null) return false;
            var original = type.OriginalDefinition;
            return original.ContainingNamespace is { } ns
                   && ns.ToDisplayString() == "System.Threading.Tasks"
                   && (original.Name == "Task");
        }

        /// <summary>
        /// Returns true if the type is System.Threading.Tasks.ValueTask or ValueTask&lt;T&gt;.
        /// </summary>
        public static bool IsValueTask(ITypeSymbol? type)
        {
            if (type is null) return false;
            var original = type.OriginalDefinition;
            return original.ContainingNamespace is { } ns
                   && ns.ToDisplayString() == "System.Threading.Tasks"
                   && (original.Name == "ValueTask");
        }

        /// <summary>
        /// Returns true if the type is a generic task type (Task&lt;T&gt; or ValueTask&lt;T&gt;).
        /// </summary>
        public static bool IsGenericTaskLike(ITypeSymbol? type)
        {
            if (type is not INamedTypeSymbol named || !named.IsGenericType) return false;
            return IsTaskLike(type);
        }

        /// <summary>
        /// Gets a display string for the Task type argument, e.g. "&lt;int&gt;" or empty for non-generic Task.
        /// </summary>
        public static string GetTypeArgumentDisplay(ITypeSymbol? type)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } named && named.TypeArguments.Length == 1)
            {
                return $"<{named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>";
            }
            return "";
        }

        /// <summary>
        /// Gets the underlying value type name for a Task type.
        /// For Task&lt;int&gt; returns "int", for Task returns "void".
        /// </summary>
        public static string GetUnderlyingTypeName(ITypeSymbol? type)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } named && named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            return "void";
        }

        /// <summary>
        /// Returns true if the method has the [AllowSynchronousIO] attribute.
        /// </summary>
        public static bool HasAllowSynchronousIOAttribute(IMethodSymbol? method)
        {
            return HasAttribute(method, "MissingAwaitDetector.AllowSynchronousIOAttribute");
        }

        /// <summary>
        /// Returns true if the method has the [FireAndForget] attribute.
        /// </summary>
        public static bool HasFireAndForgetAttribute(IMethodSymbol? method)
        {
            return HasAttribute(method, "MissingAwaitDetector.FireAndForgetAttribute");
        }

        /// <summary>
        /// Returns true if the method is the program entry point (Main method).
        /// </summary>
        public static bool IsMainMethod(IMethodSymbol? method)
        {
            if (method is null) return false;
            return method.Name == "Main" && method.IsStatic
                   && (method.ContainingType?.Name == "Program"
                       || method.ContainingType is { } ct && ct.GetMembers("Main").Length > 0 && method.ContainingType.IsStatic);
        }

        /// <summary>
        /// Returns true if the containing method is async.
        /// </summary>
        public static bool IsContainingMethodAsync(IMethodSymbol? method)
        {
            return method?.IsAsync == true;
        }

        private static bool HasAttribute(IMethodSymbol? method, string fullyQualifiedName)
        {
            if (method is null) return false;
            foreach (var attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == fullyQualifiedName)
                    return true;
            }
            return false;
        }
    }
}
