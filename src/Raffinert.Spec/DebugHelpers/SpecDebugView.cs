using System.Linq.Expressions;

namespace Raffinert.Spec.DebugHelpers;

internal class SpecDebugView(ISpec spec)
{
    public LambdaExpression Expression => spec.GetExpression();
}