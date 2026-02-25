using System;
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
    /// Code fix provider for MAWT001, MAWT002, MAWT005, MAWT008.
    /// Offers to add 'await' and make the containing method async.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitTaskCodeFixProvider))]
    [Shared]
    public sealed class AwaitTaskCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticIds.TaskTreatedAsValue,
                DiagnosticIds.SynchronousUnwrap,
                DiagnosticIds.StoredNeverAwaited,
                DiagnosticIds.SyncUnwrapInAsyncMethod);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add 'await' and make method async",
                    createChangedDocument: ct => AddAwaitAsync(context.Document, node, ct),
                    equivalenceKey: "AddAwait"),
                diagnostic);
        }

        private static async Task<Document> AddAwaitAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null) return document;

            var expressionToAwait = FindExpressionToAwait(node);
            if (expressionToAwait is null) return document;

            // Handle .Result -> await
            if (node is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.Text == "Result")
            {
                var awaitExpression = SyntaxFactory.AwaitExpression(memberAccess.Expression)
                    .WithLeadingTrivia(memberAccess.GetLeadingTrivia())
                    .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

                root = root.ReplaceNode(memberAccess, awaitExpression);
            }
            // Handle .Wait() -> await
            else if (node is InvocationExpressionSyntax invocation
                     && invocation.Expression is MemberAccessExpressionSyntax waitMemberAccess
                     && (waitMemberAccess.Name.Identifier.Text == "Wait"))
            {
                var awaitExpression = SyntaxFactory.AwaitExpression(waitMemberAccess.Expression)
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());

                root = root.ReplaceNode(invocation, awaitExpression);
            }
            // Handle .GetAwaiter().GetResult() -> await
            else if (node is InvocationExpressionSyntax getResultInvocation
                     && getResultInvocation.Expression is MemberAccessExpressionSyntax getResultMa
                     && getResultMa.Name.Identifier.Text == "GetResult"
                     && getResultMa.Expression is InvocationExpressionSyntax getAwaiterInvocation
                     && getAwaiterInvocation.Expression is MemberAccessExpressionSyntax getAwaiterMa)
            {
                var awaitExpression = SyntaxFactory.AwaitExpression(getAwaiterMa.Expression)
                    .WithLeadingTrivia(getResultInvocation.GetLeadingTrivia())
                    .WithTrailingTrivia(getResultInvocation.GetTrailingTrivia());

                root = root.ReplaceNode(getResultInvocation, awaitExpression);
            }
            // General case: wrap expression in await
            else if (expressionToAwait is ExpressionSyntax expr)
            {
                var awaitExpression = SyntaxFactory.AwaitExpression(expr.WithoutLeadingTrivia())
                    .WithLeadingTrivia(expr.GetLeadingTrivia())
                    .WithTrailingTrivia(expr.GetTrailingTrivia());

                root = root.ReplaceNode(expr, awaitExpression);
            }

            // Make containing method async if not already
            // Use FindToken to locate a node in the modified tree — the original node's span
            // may be invalid after tree transformations.
            var anchorToken = root.FindToken(Math.Min(node.SpanStart, root.FullSpan.Length - 1));
            root = MakeContainingMethodAsync(root, anchorToken.Parent ?? root);

            return document.WithSyntaxRoot(root);
        }

        private static SyntaxNode? FindExpressionToAwait(SyntaxNode node)
        {
            if (node is ExpressionSyntax)
                return node;

            if (node is VariableDeclaratorSyntax declarator)
                return declarator.Initializer?.Value;

            return node;
        }

        private static SyntaxNode MakeContainingMethodAsync(SyntaxNode root, SyntaxNode node)
        {
            var current = node;
            while (current is not null)
            {
                if (current is MethodDeclarationSyntax method && !method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    // When there are no existing modifiers, the return type carries the
                    // leading trivia (newline + indentation). Transfer it to the new
                    // async keyword so the formatting stays correct.
                    var returnTypeLeadingTrivia = method.ReturnType.GetLeadingTrivia();
                    var hasExistingModifiers = method.Modifiers.Count > 0;

                    var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space);

                    if (!hasExistingModifiers)
                    {
                        asyncModifier = asyncModifier.WithLeadingTrivia(returnTypeLeadingTrivia);
                    }

                    var newModifiers = method.Modifiers.Add(asyncModifier);

                    // Update return type: void -> Task, T -> Task<T>
                    var newReturnType = method.ReturnType;
                    // When we added async as the first token, strip the original leading
                    // trivia from the return type (it now lives on the async keyword).
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
                    else if (!IsTaskType(method.ReturnType))
                    {
                        var typeName = method.ReturnType.ToString().Trim();
                        newReturnType = SyntaxFactory.ParseTypeName($"Task<{typeName}>")
                            .WithLeadingTrivia(newLeading)
                            .WithTrailingTrivia(method.ReturnType.GetTrailingTrivia());
                    }

                    var newMethod = method
                        .WithModifiers(newModifiers)
                        .WithReturnType(newReturnType);

                    return root.ReplaceNode(method, newMethod);
                }

                if (current is LocalFunctionStatementSyntax localFunc && !localFunc.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space);

                    var newModifiers = localFunc.Modifiers.Add(asyncModifier);

                    var newLocalFunc = localFunc.WithModifiers(newModifiers);
                    return root.ReplaceNode(localFunc, newLocalFunc);
                }

                current = current.Parent;
            }

            return root;
        }

        private static bool IsTaskType(TypeSyntax type)
        {
            var name = type.ToString().Trim();
            return name == "Task" || name.StartsWith("Task<")
                || name == "ValueTask" || name.StartsWith("ValueTask<");
        }
    }
}
