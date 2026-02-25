using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MissingAwaitDetector.Analyzers.CodeFixes
{
    /// <summary>
    /// Code fix provider for MAWT004 (fire-and-forget).
    /// Offers multiple options: await, explicit discard, store in variable.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FireAndForgetCodeFixProvider))]
    [Shared]
    public sealed class FireAndForgetCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticIds.FireAndForget);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Option 1: Add await
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add 'await' and make method async",
                    createChangedDocument: ct => AddAwaitAsync(context.Document, node, ct),
                    equivalenceKey: "AddAwaitFireForget"),
                diagnostic);

            // Option 2: Explicit discard
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use explicit discard '_ ='",
                    createChangedDocument: ct => AddDiscardAsync(context.Document, node, ct),
                    equivalenceKey: "AddDiscard"),
                diagnostic);
        }

        private static async Task<Document> AddAwaitAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            if (node is not ExpressionSyntax expression) return document;

            var awaitExpression = SyntaxFactory.AwaitExpression(expression.WithoutLeadingTrivia())
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(expression, awaitExpression);

            // Make containing method async
            var anchorToken = newRoot.FindToken(node.SpanStart);
            var method = anchorToken.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method is not null && !method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                var returnTypeLeadingTrivia = method.ReturnType.GetLeadingTrivia();
                var hasExistingModifiers = method.Modifiers.Count > 0;

                var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);

                if (!hasExistingModifiers)
                {
                    asyncModifier = asyncModifier.WithLeadingTrivia(returnTypeLeadingTrivia);
                }

                var newModifiers = method.Modifiers.Add(asyncModifier);

                var newReturnType = method.ReturnType;
                var newLeading = hasExistingModifiers
                    ? returnTypeLeadingTrivia
                    : SyntaxTriviaList.Empty;

                if (method.ReturnType is PredefinedTypeSyntax predefined
                    && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    newReturnType = SyntaxFactory.ParseTypeName("Task")
                        .WithLeadingTrivia(newLeading)
                        .WithTrailingTrivia(method.ReturnType.GetTrailingTrivia());
                }

                newRoot = newRoot.ReplaceNode(method, method
                    .WithModifiers(newModifiers)
                    .WithReturnType(newReturnType));
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> AddDiscardAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            if (node.Parent is not ExpressionStatementSyntax exprStatement) return document;

            // Replace: ProcessAsync(); -> _ = ProcessAsync();
            var discardAssignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("_"),
                    ((ExpressionStatementSyntax)exprStatement).Expression))
                .WithLeadingTrivia(exprStatement.GetLeadingTrivia())
                .WithTrailingTrivia(exprStatement.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(exprStatement, discardAssignment);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
