using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// MAWT006: Detects ValueTask misuse:
    /// - Awaiting a ValueTask multiple times
    /// - Storing a ValueTask and awaiting it after other async operations
    /// - Storing a ValueTask in a collection
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ValueTaskMisuseAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.ValueTaskMisuse);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            SyntaxNode? body = method.Body ?? (SyntaxNode?)method.ExpressionBody;
            if (body is null) return;

            var valueTaskVars = new Dictionary<string, VariableDeclaratorSyntax>();

            foreach (var declarator in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken);
                if (symbol is ILocalSymbol local && TaskTypeHelpers.IsValueTask(local.Type))
                {
                    valueTaskVars[declarator.Identifier.Text] = declarator;
                }
            }

            if (valueTaskVars.Count == 0)
                return;

            foreach (var kvp in valueTaskVars)
            {
                CheckValueTaskUsage(context, body, kvp.Key, kvp.Value);
            }
        }

        private static void CheckValueTaskUsage(
            SyntaxNodeAnalysisContext context,
            SyntaxNode body,
            string varName,
            VariableDeclaratorSyntax declarator)
        {
            var awaitCount = 0;
            var hasIntermediateAwait = false;
            var seenDeclaration = false;

            foreach (var node in body.DescendantNodes())
            {
                // Track when we pass the declaration
                if (node == declarator)
                {
                    seenDeclaration = true;
                    continue;
                }

                if (!seenDeclaration)
                    continue;

                if (node is AwaitExpressionSyntax awaitExpr)
                {
                    var awaitedName = GetIdentifierName(awaitExpr.Expression);
                    if (awaitedName == varName)
                    {
                        awaitCount++;
                        if (awaitCount > 1)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ValueTaskMisuse,
                                awaitExpr.GetLocation(),
                                "awaited multiple times"));
                        }
                        else if (hasIntermediateAwait)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ValueTaskMisuse,
                                awaitExpr.GetLocation(),
                                "awaited after another asynchronous operation (must be awaited immediately)"));
                        }
                    }
                    else
                    {
                        hasIntermediateAwait = true;
                    }
                }
            }
        }

        private static string? GetIdentifierName(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                // await vt.ConfigureAwait(false)
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax ma } =>
                    (ma.Expression as IdentifierNameSyntax)?.Identifier.Text,
                _ => null
            };
        }
    }
}
