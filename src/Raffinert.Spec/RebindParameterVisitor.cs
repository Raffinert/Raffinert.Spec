using System.Linq.Expressions;

namespace Raffinert.Spec;

internal class RebindParameterVisitor(ParameterExpression oldParameter, Expression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == oldParameter
            ? newParameter
            : base.VisitParameter(node);
    }
}