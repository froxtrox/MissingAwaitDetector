using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// MAWT002: Detects synchronous unwrapping of Task via .Result, .Wait(), .GetAwaiter().GetResult().
    /// MAWT008: Same but specifically inside async methods (escalated severity).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SynchronousUnwrapAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.SynchronousUnwrap,
                DiagnosticDescriptors.SyncUnwrapInAsyncMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Detects .Result property access on Task types.
        /// </summary>
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            if (memberAccess.Name.Identifier.Text != "Result")
                return;

            var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
            if (!TaskTypeHelpers.IsTaskLike(receiverType))
                return;

            // .Result inside ContinueWith is allowed (the task is already complete)
            if (IsInsideContinueWith(memberAccess))
                return;

            ReportSyncUnwrap(context, memberAccess.GetLocation(), "Result", memberAccess);
        }

        /// <summary>
        /// Detects .Wait() and .GetAwaiter().GetResult() calls on Task types.
        /// </summary>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName == "Wait")
            {
                var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
                if (TaskTypeHelpers.IsTaskLike(receiverType))
                {
                    ReportSyncUnwrap(context, invocation.GetLocation(), "Wait()", invocation);
                }
            }
            else if (methodName == "GetResult")
            {
                // Check for .GetAwaiter().GetResult() chain
                if (memberAccess.Expression is InvocationExpressionSyntax innerInvocation
                    && innerInvocation.Expression is MemberAccessExpressionSyntax innerMemberAccess
                    && innerMemberAccess.Name.Identifier.Text == "GetAwaiter")
                {
                    var receiverType = context.SemanticModel.GetTypeInfo(innerMemberAccess.Expression, context.CancellationToken).Type;
                    if (TaskTypeHelpers.IsTaskLike(receiverType))
                    {
                        ReportSyncUnwrap(context, invocation.GetLocation(), "GetAwaiter().GetResult()", invocation);
                    }
                }
            }
        }

        private static void ReportSyncUnwrap(SyntaxNodeAnalysisContext context, Location location, string accessor, SyntaxNode node)
        {
            var containingMethod = GetContainingMethod(context.SemanticModel, node, context.CancellationToken);

            if (TaskTypeHelpers.HasAllowSynchronousIOAttribute(containingMethod))
                return;

            // Main() is allowed to block synchronously
            if (TaskTypeHelpers.IsMainMethod(containingMethod))
                return;

            // Inside an async method, MAWT008 is more specific and escalated
            if (TaskTypeHelpers.IsContainingMethodAsync(containingMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.SyncUnwrapInAsyncMethod,
                    location,
                    accessor));
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SynchronousUnwrap,
                location,
                accessor));
        }

        private static IMethodSymbol? GetContainingMethod(SemanticModel semanticModel, SyntaxNode node, System.Threading.CancellationToken ct)
        {
            var current = node.Parent;
            while (current is not null)
            {
                if (current is MethodDeclarationSyntax methodDecl)
                    return semanticModel.GetDeclaredSymbol(methodDecl, ct) as IMethodSymbol;
                if (current is LocalFunctionStatementSyntax localFunc)
                    return semanticModel.GetDeclaredSymbol(localFunc, ct) as IMethodSymbol;
                if (current is AnonymousFunctionExpressionSyntax)
                    break; // Don't escape lambdas
                current = current.Parent;
            }
            return null;
        }

        private static bool IsInsideContinueWith(SyntaxNode node)
        {
            var current = node.Parent;
            while (current is not null)
            {
                if (current is InvocationExpressionSyntax invocation
                    && invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && memberAccess.Name.Identifier.Text == "ContinueWith")
                {
                    return true;
                }
                if (current is MethodDeclarationSyntax || current is LocalFunctionStatementSyntax)
                    break;
                current = current.Parent;
            }
            return false;
        }
    }
}
