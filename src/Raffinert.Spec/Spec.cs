using Raffinert.Spec.Debug;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Raffinert.Spec;

[DebuggerDisplay("{GetExpression()}")]
[DebuggerTypeProxy(typeof(SpecDebugView<>))]
public abstract class Spec<T> : ISpec
{
    public abstract Expression<Func<T, bool>> GetExpression();

    LambdaExpression ISpec.GetExpression() => GetExpression();

    public static Spec<T> Create(Expression<Func<T, bool>> expression)
    {
        return new InlineSpec<T>(expression);
    }

    public virtual bool IsSatisfiedBy(T candidate) => GetCompiledExpression()(candidate);

    public static Spec<T> True()
    {
        return new TrueSpec<T>();
    }

    public static Spec<T> False()
    {
        return new FalseSpec<T>();
    }

    public Spec<T> And(Expression<Func<T, bool>> expression)
    {
        return new AndSpecForExpression<T>(this, expression);
    }

    public Spec<T> And(Spec<T> spec)
    {
        return new AndSpec<T>(this, spec);
    }

    public Spec<T> Or(Expression<Func<T, bool>> expression)
    {
        return new OrSpecForExpression<T>(this, expression);
    }

    public Spec<T> Or(Spec<T> spec)
    {
        return new OrSpec<T>(this, spec);
    }

    public Spec<T> Not()
    {
        return new NotSpec<T>(this);
    }

    public static Spec<T> operator &(Spec<T> left, Spec<T> right)
    {
        return new AndSpec<T>(left, right);
    }

    public static Spec<T> operator |(Spec<T> left, Spec<T> right)
    {
        return new OrSpec<T>(left, right);
    }

    public static Spec<T> operator !(Spec<T> spec)
    {
        return new NotSpec<T>(spec);
    }

    public static bool operator true(Spec<T> spec)
    {
        return false;
    }

    public static bool operator false(Spec<T> spec)
    {
        return false;
    }

    public static implicit operator Expression<Func<T, bool>>(Spec<T> spec)
    {
        return spec.GetExpression();
    }

    public static implicit operator Func<T, bool>(Spec<T> spec)
    {
        return spec.GetCompiledExpression();
    }

    private Func<T, bool>? _compiledExpression;
    private Func<T, bool> GetCompiledExpression()
    {
        return _compiledExpression ??= GetExpression().Compile();
    }
}

internal interface ISpec
{
    LambdaExpression GetExpression();
}

file sealed class TrueSpec<T> : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression() => static p => true;
}

file sealed class FalseSpec<T> : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression() => static p => false;
}

file sealed class InlineSpec<T>(Expression<Func<T, bool>> expression) : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        return (Expression<Func<T, bool>>)new IsSatisfiedByCallVisitor().Visit(expression)!;
    }
}

file sealed class AndSpec<T>(Spec<T> leftSpec, Spec<T> rightSpec) : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        return ExpressionBuilder.BuildAndExpression(leftSpec.GetExpression(), rightSpec.GetExpression());
    }
}

file sealed class AndSpecForExpression<T>(Spec<T> leftSpec, Expression<Func<T, bool>> rightExpression)
    : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        var rewrittenRightExpression = (Expression<Func<T, bool>>)new IsSatisfiedByCallVisitor().Visit(rightExpression)!;
        return ExpressionBuilder.BuildAndExpression(leftSpec.GetExpression(), rewrittenRightExpression);
    }
}
file sealed class NotSpec<T>(Spec<T> spec) : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        return ExpressionBuilder.BuildNotExpression(spec.GetExpression());
    }
}

file sealed class OrSpec<T>(Spec<T> leftSpec, Spec<T> rightSpec) : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        return ExpressionBuilder.BuildOrExpression(leftSpec.GetExpression(), rightSpec.GetExpression());
    }
}

file sealed class OrSpecForExpression<T>(Spec<T> leftSpec, Expression<Func<T, bool>> rightExpression)
    : Spec<T>
{
    public override Expression<Func<T, bool>> GetExpression()
    {
        var rewrittenRightExpression = (Expression<Func<T, bool>>)new IsSatisfiedByCallVisitor().Visit(rightExpression)!;
        return ExpressionBuilder.BuildOrExpression(leftSpec.GetExpression(), rewrittenRightExpression);
    }
}

file static class ExpressionBuilder
{
    public static Expression<Func<T, bool>> BuildNotExpression<T>(Expression<Func<T, bool>> expression)
    {
        var negated = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
    }

    public static Expression<Func<T, bool>> BuildOrExpression<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var rightExpressionBody = new RebindParameterVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightExpressionBody), left.Parameters);
    }

    public static Expression<Func<T, bool>> BuildAndExpression<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var rightExpressionBody = new RebindParameterVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightExpressionBody), left.Parameters);
    }
}

file sealed class RebindParameterVisitor(ParameterExpression oldParameter, Expression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == oldParameter
            ? newParameter
            : base.VisitParameter(node);
    }
}

file sealed class IsSatisfiedByCallVisitor : ExpressionVisitor
{
    // Visits method group expressions like `Spec<Category>.Create(category => category.Products.Any(productSpec.IsSatisfiedBy))`
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType != ExpressionType.Convert) return base.VisitUnary(node);

        if (node.Operand is not MethodCallExpression mcx
            || mcx.Method.Name != nameof(Delegate.CreateDelegate)
            || mcx.Arguments.Count != 2
            || mcx.Arguments[1] is not MemberExpression memberExpr)
        {
            return base.VisitUnary(node);
        }

        if (memberExpr is not { Expression: ConstantExpression constantExpression, Member: FieldInfo fieldInfo })
        {
            return base.VisitUnary(node);
        }

        var container = constantExpression.Value;
        var value = fieldInfo.GetValue(container);

        if (!typeof(ISpec).IsAssignableFrom(value.GetType()))
        {
            return base.VisitUnary(node);
        }

        var specExpression = ((ISpec)value).GetExpression();

        return specExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType?.IsGenericType != true
            || node.Method.DeclaringType?.GetGenericTypeDefinition() != typeof(Spec<>)
            || node.Method.Name != nameof(Spec<object>.IsSatisfiedBy)
            || node.Object is not MemberExpression memberExpr)
        {
            return base.VisitMethodCall(node);
        }

        if (memberExpr is not { Expression: ConstantExpression constantExpression, Member: FieldInfo fieldInfo })
        {
            return base.VisitMethodCall(node);
        }

        var container = constantExpression.Value;
        var value = fieldInfo.GetValue(container);

        if (!typeof(ISpec).IsAssignableFrom(value.GetType()))
        {
            return base.VisitMethodCall(node);
        }

        var specExpression = ((ISpec)value).GetExpression();

        var paramReplacer = new RebindParameterVisitor(specExpression.Parameters[0], node.Arguments[0]);

        if (node.Arguments[0].NodeType == ExpressionType.MemberAccess)
        {
            var body = paramReplacer.Visit(specExpression.Body)!;
            return body;
        }

        var lambda = (LambdaExpression)paramReplacer.Visit(specExpression)!;

        return lambda.Body;
    }
}