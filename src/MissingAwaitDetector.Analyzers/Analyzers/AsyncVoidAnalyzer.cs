using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MissingAwaitDetector.Analyzers
{
    /// <summary>
    /// MAWT010: Detects async void methods. Async void methods are dangerous because
    /// unhandled exceptions will crash the process. The only valid use is for event handlers.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncVoidAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
               ImmutableArray.Create(DiagnosticDescriptors.AsyncVoidMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            if (method.ReturnType is not PredefinedTypeSyntax predefined
                || !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                return;

            // Allow async void event handlers: methods whose parameters match
            // the (object sender, EventArgs e) pattern or have a single EventArgs-derived parameter.
            var symbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
            if (symbol is not null && IsEventHandler(symbol, context.SemanticModel.Compilation))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AsyncVoidMethod,
                method.Identifier.GetLocation(),
                method.Identifier.Text));
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (LocalFunctionStatementSyntax)context.Node;

            if (!localFunc.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            if (localFunc.ReturnType is not PredefinedTypeSyntax predefined
                || !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AsyncVoidMethod,
                localFunc.Identifier.GetLocation(),
                localFunc.Identifier.Text));
        }

        private static bool IsEventHandler(IMethodSymbol method, Compilation compilation)
        {
            if (method.Parameters.Length != 2)
                return false;

            var firstParam = method.Parameters[0];
            var secondParam = method.Parameters[1];

            // First parameter must be object
            if (firstParam.Type.SpecialType != SpecialType.System_Object)
                return false;

            // Second parameter must derive from System.EventArgs
            var eventArgsType = compilation.GetTypeByMetadataName("System.EventArgs");
            if (eventArgsType is null)
                return false;

            return IsOrDerivedFrom(secondParam.Type, eventArgsType);
        }

        private static bool IsOrDerivedFrom(ITypeSymbol type, ITypeSymbol baseType)
        {
            var current = type;
            while (current is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;
                current = current.BaseType;
            }
            return false;
        }
    }
}
