using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            messageFormat: "The first argument of SpecTemplate.Create must be either an anonymous type projection or class with matching properties (e.g., 'p => new { p.Name }').",
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

            if (lambdaExpr.Body is AnonymousObjectCreationExpressionSyntax)
            {
            }
            else if (lambdaExpr.Body is ObjectCreationExpressionSyntax objectCreationExpr)
            {
                // Validate that the object initializer's properties match TSample's properties
                ValidateObjectInitializer(context, objectCreationExpr, methodSymbol.ContainingType.TypeArguments[0]);
            }
            else
            {
                var diagnostic = Diagnostic.Create(
                    InvalidTemplateUsageRule,
                    firstArgExpression.GetLocation()
                );

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void ValidateObjectInitializer(
            SyntaxNodeAnalysisContext context,
            ObjectCreationExpressionSyntax objectCreationExpr,
            ITypeSymbol sampleTypeSymbol)
        {
            // Get all public instance properties of TSample
            var sampleProperties = new HashSet<string>(sampleTypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Select(p => p.Name));

            foreach (var initializer in objectCreationExpr.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
            {
                if (initializer.Left is IdentifierNameSyntax memberName)
                {
                    var assignedMember = memberName.Identifier.Text;

                    // If the assigned property does not exist in TSample, report an error
                    if (!sampleProperties.Contains(assignedMember))
                    {
                        var diagnostic = Diagnostic.Create(
                            InvalidTemplateUsageRule,
                            memberName.GetLocation(),
                            $"Property '{assignedMember}' does not exist in sample type '{sampleTypeSymbol.Name}'."
                        );

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
