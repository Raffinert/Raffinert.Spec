using System.Linq.Expressions;

namespace Raffinert.Spec.Debug;

internal class SpecDebugView<T>(Spec<T> spec)
{
    public Expression<Func<T, bool>> Expression => spec.GetExpression();
}