using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// MAWT004: Detects fire-and-forget calls to Task-returning methods
    /// where the result is neither awaited, stored, nor assigned.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FireAndForgetAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [DiagnosticDescriptors.FireAndForget];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
        }

        private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context)
        {
            var expressionStatement = (ExpressionStatementSyntax)context.Node;
            var expression = expressionStatement.Expression;

            if (expression is not InvocationExpressionSyntax
                && expression is not ConditionalAccessExpressionSyntax)
                return;

            var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
            if (!TaskTypeHelpers.IsTaskLike(typeInfo.Type))
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
            if (symbolInfo.Symbol is IMethodSymbol calledMethod
                && TaskTypeHelpers.HasFireAndForgetAttribute(calledMethod))
                return;

            var methodName = GetMethodName(expression);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FireAndForget,
                expression.GetLocation(),
                methodName));
        }

        private static string GetMethodName(ExpressionSyntax expression)
        {
            return expression switch
            {
                InvocationExpressionSyntax invocation => invocation.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    _ => expression.ToString()
                },
                ConditionalAccessExpressionSyntax conditional => conditional.ToString(),
                _ => expression.ToString()
            };
        }
    }
}
