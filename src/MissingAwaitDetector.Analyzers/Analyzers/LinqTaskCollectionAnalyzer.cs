using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers.Analyzers
{
    /// <summary>
    /// MAWT007: Detects LINQ expressions that produce collections of Tasks
    /// without composing them via Task.WhenAll/WhenAny.
    /// Example: list.Select(x => ProcessAsync(x)).ToList() produces List&lt;Task&gt; not List&lt;Result&gt;.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LinqTaskCollectionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [DiagnosticDescriptors.LinqTaskCollection];

        private static readonly ImmutableHashSet<string> MaterializingMethods = ImmutableHashSet.Create(
            "ToList", "ToArray", "ToDictionary", "ToHashSet", "ToImmutableArray",
            "ToImmutableList", "ToImmutableHashSet");

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName != "Select" && methodName != "SelectMany")
                return;

            var typeInfo = context.SemanticModel.GetTypeInfo(invocation, context.CancellationToken);
            if (typeInfo.Type is not INamedTypeSymbol returnType)
                return;

            if (!IsEnumerableOfTask(returnType))
                return;

            if (IsComposedWithTaskWhen(invocation))
                return;

            // Check if materialized (ToList, ToArray) before being passed to WhenAll
            var parent = invocation.Parent;
            if (parent is MemberAccessExpressionSyntax parentMa
                && parentMa.Parent is InvocationExpressionSyntax parentInvocation)
            {
                var parentMethodName = parentMa.Name.Identifier.Text;
                if (MaterializingMethods.Contains(parentMethodName))
                {
                    if (IsComposedWithTaskWhen(parentInvocation))
                        return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LinqTaskCollection,
                invocation.GetLocation()));
        }

        private static bool IsEnumerableOfTask(ITypeSymbol type)
        {
            // Check IEnumerable<Task<T>>, List<Task<T>>, Task<T>[], etc.
            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var typeArg in named.TypeArguments)
                {
                    if (TaskTypeHelpers.IsTaskLike(typeArg))
                        return true;
                }
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (iface.IsGenericType && iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                {
                    foreach (var typeArg in iface.TypeArguments)
                    {
                        if (TaskTypeHelpers.IsTaskLike(typeArg))
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool IsComposedWithTaskWhen(SyntaxNode node)
        {
            var parent = node.Parent;

            // Direct: Task.WhenAll(list.Select(...))
            if (parent is ArgumentSyntax arg
                && arg.Parent is ArgumentListSyntax argList
                && argList.Parent is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                var name = ma.Name.Identifier.Text;
                if (name == "WhenAll" || name == "WhenAny")
                    return true;
            }

            // Chained: await Task.WhenAll(...)
            if (parent is AwaitExpressionSyntax)
                return true;

            return false;
        }
    }
}
