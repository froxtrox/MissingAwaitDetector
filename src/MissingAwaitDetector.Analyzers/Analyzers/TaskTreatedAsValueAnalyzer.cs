using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// MAWT001: Detects when a Task or Task&lt;T&gt; is used as if it were the underlying value type.
    /// Examples: task.ToString(), Console.WriteLine(task), $"{task}", passing Task&lt;T&gt; where T expected.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaskTreatedAsValueAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.TaskTreatedAsValue);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInterpolation, SyntaxKind.Interpolation);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeArgument, SyntaxKind.Argument);
        }

        /// <summary>
        /// Detects $"{task}" - string interpolation with Task.
        /// </summary>
        private static void AnalyzeInterpolation(SyntaxNodeAnalysisContext context)
        {
            var interpolation = (InterpolationSyntax)context.Node;
            var typeInfo = context.SemanticModel.GetTypeInfo(interpolation.Expression, context.CancellationToken);

            if (TaskTypeHelpers.IsTaskLike(typeInfo.Type))
            {
                var typeArg = TaskTypeHelpers.GetTypeArgumentDisplay(typeInfo.Type);
                var underlying = TaskTypeHelpers.GetUnderlyingTypeName(typeInfo.Type);
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TaskTreatedAsValue,
                    interpolation.GetLocation(),
                    typeArg,
                    underlying));
            }
        }

        /// <summary>
        /// Detects task.ToString(), task.GetHashCode(), task.Equals(...).
        /// </summary>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "ToString" && methodName != "GetHashCode" && methodName != "Equals")
                return;

            var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
            if (!TaskTypeHelpers.IsTaskLike(receiverType))
                return;

            var typeArg = TaskTypeHelpers.GetTypeArgumentDisplay(receiverType);
            var underlying = TaskTypeHelpers.GetUnderlyingTypeName(receiverType);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TaskTreatedAsValue,
                invocation.GetLocation(),
                typeArg,
                underlying));
        }

        /// <summary>
        /// Detects passing Task&lt;T&gt; as an argument where T (or other non-Task type) is expected.
        /// </summary>
        private static void AnalyzeArgument(SyntaxNodeAnalysisContext context)
        {
            var argument = (ArgumentSyntax)context.Node;
            var argType = context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type;

            if (!TaskTypeHelpers.IsTaskLike(argType))
                return;

            if (argument.Parent is not ArgumentListSyntax argList)
                return;

            if (argList.Parent is not InvocationExpressionSyntax invocation)
                return;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol is null)
                return;

            var argIndex = argList.Arguments.IndexOf(argument);
            if (argIndex < 0 || argIndex >= methodSymbol.Parameters.Length)
                return;

            var paramType = methodSymbol.Parameters[argIndex].Type;

            // If the parameter also expects a Task type, that's fine
            if (TaskTypeHelpers.IsTaskLike(paramType))
                return;

            // If the parameter is object, skip (Console.WriteLine(object) is covered by interpolation/ToString)
            if (paramType.SpecialType == SpecialType.System_Object)
                return;

            // If param is Func<Task>/Action or delegate, skip
            if (paramType.TypeKind == TypeKind.Delegate)
                return;

            var typeArg = TaskTypeHelpers.GetTypeArgumentDisplay(argType);
            var underlying = TaskTypeHelpers.GetUnderlyingTypeName(argType);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TaskTreatedAsValue,
                argument.GetLocation(),
                typeArg,
                underlying));
        }
    }
}
