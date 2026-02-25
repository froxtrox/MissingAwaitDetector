using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers.Analyzers
{
    /// <summary>
    /// MAWT003: Detects Task state inspection and diagnostic property access:
    /// .Status, .IsCompleted, .IsFaulted, .IsCompletedSuccessfully, .IsCanceled, .Exception,
    /// .Id, .CurrentId, .AsyncState, .CreationOptions.
    /// <para>
    /// End users can opt out specific properties via .editorconfig:
    /// <c>dotnet_diagnostic.MAWT003.excluded_task_properties = Id, CurrentId</c>
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaskStateInspectionAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>All Task properties flagged by default.</summary>
        private static readonly ImmutableHashSet<string> DefaultStateProperties = ImmutableHashSet.Create(
            // State properties
            "Status",
            "IsCompleted",
            "IsFaulted",
            "IsCompletedSuccessfully",
            "IsCanceled",
            "Exception",
            // Diagnostic/metadata properties — not meaningful in application code
            "Id",
            "CurrentId",
            "AsyncState",
            "CreationOptions");

        /// <summary>editorconfig key for per-property opt-outs.</summary>
        private const string ExcludedPropertiesKey = "dotnet_diagnostic.MAWT003.excluded_task_properties";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.TaskStateInspection);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var propertyName = memberAccess.Name.Identifier.Text;

            // Stage 1: fast O(1) exit for the vast majority of member accesses
            if (!DefaultStateProperties.Contains(propertyName))
                return;

            // Skip .Result (handled by MAWT002)
            if (propertyName == "Result")
                return;

            // Stage 2: consult editorconfig for per-property opt-outs
            var configOptions = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);
            if (!GetEffectiveProperties(configOptions).Contains(propertyName))
                return;

            // Stage 3: verify the receiver is actually a Task-like type
            var receiverType = context.SemanticModel
                .GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;

            if (receiverType != null)
            {
                if (!TaskTypeHelpers.IsTaskLike(receiverType))
                    return;
            }
            else
            {
                // Static access: Task.CurrentId
                // GetTypeInfo returns null when the receiver is a type name, not an expression.
                var receiverSymbol = context.SemanticModel
                    .GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol;

                if (receiverSymbol is not INamedTypeSymbol typeSymbol)
                    return;

                if (typeSymbol.Name != "Task" ||
                    typeSymbol.ContainingNamespace?.ToDisplayString() != "System.Threading.Tasks")
                    return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TaskStateInspection,
                memberAccess.Name.GetLocation(),
                propertyName));
        }

        /// <summary>
        /// Returns the set of properties to flag for this file, after applying any
        /// <c>dotnet_diagnostic.MAWT003.excluded_task_properties</c> exclusions from .editorconfig.
        /// </summary>
        private static ImmutableHashSet<string> GetEffectiveProperties(AnalyzerConfigOptions options)
        {
            if (options.TryGetValue(ExcludedPropertiesKey, out var excluded)
                && !string.IsNullOrWhiteSpace(excluded))
            {
                var toExclude = excluded
                    .Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0);

                return DefaultStateProperties.Except(toExclude);
            }

            return DefaultStateProperties;
        }
    }
}
