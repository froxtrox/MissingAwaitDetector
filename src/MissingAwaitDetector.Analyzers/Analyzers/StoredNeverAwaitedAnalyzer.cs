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
    /// MAWT005: Detects Task variables that are stored but never awaited,
    /// returned, or composed with Task.WhenAll/WhenAny.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StoredNeverAwaitedAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.StoredNeverAwaited);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunctionBody,
                SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            SyntaxNode? body = method.Body ?? (SyntaxNode?)method.ExpressionBody;
            if (body is null) return;
            AnalyzeBody(context, body);
        }

        private static void AnalyzeLocalFunctionBody(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (LocalFunctionStatementSyntax)context.Node;
            SyntaxNode? body = localFunc.Body ?? (SyntaxNode?)localFunc.ExpressionBody;
            if (body is null) return;
            AnalyzeBody(context, body);
        }

        private static void AnalyzeBody(SyntaxNodeAnalysisContext context, SyntaxNode body)
        {
            var taskVariables = new Dictionary<string, (Location Location, ILocalSymbol Symbol)>();

            foreach (var declaration in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
                if (symbol is ILocalSymbol local && TaskTypeHelpers.IsTaskLike(local.Type))
                {
                    // Skip explicit discard
                    if (declaration.Identifier.Text == "_")
                        continue;

                    taskVariables[declaration.Identifier.Text] = (declaration.GetLocation(), local);
                }
            }

            if (taskVariables.Count == 0)
                return;

            var handledVariables = new HashSet<string>();

            foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var name = identifier.Identifier.Text;
                if (!taskVariables.ContainsKey(name))
                    continue;

                if (handledVariables.Contains(name))
                    continue;

                // Skip the declaration initializer itself
                if (identifier.Parent is EqualsValueClauseSyntax)
                    continue;

                if (IsProperlyHandled(identifier))
                    handledVariables.Add(name);
            }

            foreach (var kvp in taskVariables)
            {
                if (!handledVariables.Contains(kvp.Key))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.StoredNeverAwaited,
                        kvp.Value.Location,
                        kvp.Key));
                }
            }
        }

        private static bool IsProperlyHandled(IdentifierNameSyntax identifier)
        {
            var parent = identifier.Parent;

            if (parent is AwaitExpressionSyntax)
                return true;

            // await task.ConfigureAwait(...)
            if (parent is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Parent is InvocationExpressionSyntax invocation
                && invocation.Parent is AwaitExpressionSyntax)
                return true;

            if (parent is ReturnStatementSyntax)
                return true;
            if (parent is ArrowExpressionClauseSyntax)
                return true;

            // Task.WhenAll(task) / Task.WhenAny(task) - passed as argument
            if (parent is ArgumentSyntax arg
                && arg.Parent is ArgumentListSyntax argList
                && argList.Parent is InvocationExpressionSyntax taskInvocation)
            {
                if (taskInvocation.Expression is MemberAccessExpressionSyntax ma)
                {
                    var methodName = ma.Name.Identifier.Text;
                    if (methodName == "WhenAll" || methodName == "WhenAny" ||
                        methodName == "WaitAll" || methodName == "WaitAny" ||
                        methodName == "Add") // tasks.Add(task)
                        return true;
                }
            }

            // tasks collection initializer { task1, task2 }
            if (parent is InitializerExpressionSyntax)
                return true;

            // Assigned to array: new[] { task }
            if (parent is ArrayCreationExpressionSyntax || parent is ImplicitArrayCreationExpressionSyntax)
                return true;

            // ContinueWith
            if (parent is MemberAccessExpressionSyntax ma2 && ma2.Name.Identifier.Text == "ContinueWith")
                return true;

            return false;
        }
    }
}
