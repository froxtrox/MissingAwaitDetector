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
    /// MAWT009: Detects Task variables that are inspected or used before being returned.
    /// This is a code smell - either return the Task directly or make the method async.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaskUsedBeforeReturnAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.TaskUsedBeforeReturn);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            // Skip async methods - they naturally await
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
            if (methodSymbol?.IsAsync == true)
                return;

            if (methodSymbol is null || !TaskTypeHelpers.IsTaskLike(methodSymbol.ReturnType))
                return;

            SyntaxNode? body = method.Body ?? (SyntaxNode?)method.ExpressionBody;
            if (body is null) return;

            var returnedVars = new HashSet<string>();
            foreach (var ret in body.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                if (ret.Expression is IdentifierNameSyntax id)
                    returnedVars.Add(id.Identifier.Text);
            }

            if (returnedVars.Count == 0)
                return;

            foreach (var declarator in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                var varName = declarator.Identifier.Text;
                if (!returnedVars.Contains(varName))
                    continue;

                var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken);
                if (symbol is not ILocalSymbol local || !TaskTypeHelpers.IsTaskLike(local.Type))
                    continue;

                var usages = body.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == varName)
                    .ToList();

                var nonTrivialUsages = usages.Where(id =>
                    id.Parent is not EqualsValueClauseSyntax
                    && id.Parent is not ReturnStatementSyntax
                    && id.Parent is not VariableDeclaratorSyntax
                ).ToList();

                if (nonTrivialUsages.Count > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.TaskUsedBeforeReturn,
                        declarator.GetLocation(),
                        varName));
                }
            }
        }
    }
}
