﻿using System.Diagnostics;
using System.Linq.Expressions;
using Raffinert.Spec.Debug;

namespace Raffinert.Spec;

[DebuggerDisplay("{GetExpression()}")]
[DebuggerTypeProxy(typeof(SpecDebugView<>))]
public abstract class Spec<T>
{
    public abstract Expression<Func<T, bool>> GetExpression();

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
    public override Expression<Func<T, bool>> GetExpression() => expression;
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
        return ExpressionBuilder.BuildAndExpression(leftSpec.GetExpression(), rightExpression);
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
        return ExpressionBuilder.BuildOrExpression(leftSpec.GetExpression(), rightExpression);
    }
}

file static class ExpressionBuilder
{
    public static Expression<Func<T, bool>> BuildNotExpression<T>(Expression<Func<T, bool>> expression)
    {
        var negated = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
    }

    public static Expression<Func<T, bool>> BuildOrExpression<T>(Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var rightExpressionBody = new RebindParameterVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightExpressionBody), left.Parameters);
    }

    public static Expression<Func<T, bool>> BuildAndExpression<T>(Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var rightExpressionBody = new RebindParameterVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightExpressionBody), left.Parameters);
    }
}

file sealed class RebindParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == oldParameter
            ? newParameter
            : base.VisitParameter(node);
    }
}