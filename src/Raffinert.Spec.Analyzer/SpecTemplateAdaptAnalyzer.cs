using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Raffinert.Spec.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SpecTemplateAdaptAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MissingMembersRule = new(
        id: "SPEC001",
        title: "Type Signature Validation Failed",
        messageFormat: "Type '{0}' is missing required members {{ {1} }} for specification template.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingMembersRule);

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

        // Ensure it's a call to SpecTemplate<TSample, TTemplate>.Adapt<TN>() or ISpecTemplate<TTemplate>.Adapt<TN>()
        if ((methodSymbol.ContainingType.Name != "ISpecTemplate" && methodSymbol.ContainingType.Name != "SpecTemplate") || methodSymbol.Name != "Adapt")
            return;

        // Extract TTemplate and TN
        var tTemplate =  methodSymbol.ContainingType.TypeArguments[methodSymbol.ContainingType.Name == "ISpecTemplate" ? 0 : 1];

        if (tTemplate.TypeKind != TypeKind.Class || tTemplate.SpecialType != SpecialType.None)
            return; // Ignore if not a class or any special type

        var tN = methodSymbol.TypeArguments[0];

        var missingMembers = tTemplate.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(prop => !tN.GetMembers().OfType<IPropertySymbol>()
                .Any(tnProp => tnProp.Name == prop.Name &&
                               tnProp.Type.Equals(prop.Type, SymbolEqualityComparer.Default)))
            .Select(prop => prop.Name)
            .ToList();

        if (missingMembers.Count <= 0)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            MissingMembersRule,
            invocationExpr.GetLocation(),
            tN.Name, string.Join(", ", missingMembers));

        context.ReportDiagnostic(diagnostic);
    }
}