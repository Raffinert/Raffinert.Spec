using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Raffinert.Spec.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SpecTemplateCreateAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor InvalidTemplateUsageRule = new(
            id: "SPEC002",
            title: "Invalid SpecTemplate Usage",
            messageFormat: "The first argument of SpecTemplate.Create must be an anonymous type projection (e.g., 'p => new { p.Name }').",
            category: "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(InvalidTemplateUsageRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (semanticModel.GetSymbolInfo(invocationExpr).Symbol is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.ContainingType.Name != "SpecTemplate" || methodSymbol.Name != "Create")
                return;

            if (invocationExpr.ArgumentList.Arguments.Count < 2)
                return;

            var firstArgExpression = invocationExpr.ArgumentList.Arguments[0].Expression;

            if (firstArgExpression is not LambdaExpressionSyntax lambdaExpr)
                return;

            if (lambdaExpr.Body is not AnonymousObjectCreationExpressionSyntax)
            {
                var diagnostic = Diagnostic.Create(
                    InvalidTemplateUsageRule,
                    firstArgExpression.GetLocation()
                );

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
