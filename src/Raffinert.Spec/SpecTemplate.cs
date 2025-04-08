using System.Linq.Expressions;
using System.Reflection;

namespace Raffinert.Spec;

public static class SpecTemplate<TSample>
{
    public static SpecTemplate<TSample, TTemplate> Create<TTemplate>(Expression<Func<TSample, TTemplate>> template,
        Expression<Func<TTemplate, bool>> expression)
    {
        return new SpecTemplate<TSample, TTemplate>(template, expression);
    }
}

public class SpecTemplate<TSample, TTemplate> : ISpecTemplate<TTemplate>
{
    public Expression<Func<TSample, TTemplate>> template { get; }
    public MemberExpression[] templateMemberExpressions { get; set; }
    public Expression<Func<TTemplate, bool>>? expression { get; }
    private Expression<Func<TSample, bool>>? combinedExpression { get; set; }

    public SpecTemplate(Expression<Func<TSample, TTemplate>> template, Expression<Func<TTemplate, bool>> expression)
    {
        MemberExpression[] mex;

        switch (template.Body)
        {
            case NewExpression ne:
                mex = ne.Arguments.OfType<MemberExpression>().ToArray();
                if (mex.Length != ne.Arguments.Count) throw new ArgumentException("Template must be a NewExpression with MemberExpressions only", nameof(template));
                this.expression = expression;
                break;
            case MemberInitExpression me:
                combinedExpression = CombineExpressions(template, expression);
                mex = me.Bindings.Select(x => (x as MemberAssignment)?.Expression as MemberExpression).OfType<MemberExpression>().ToArray();
                if (mex.Length != me.Bindings.Count) throw new ArgumentException("Template must be a NewExpression with MemberExpressions only", nameof(template));
                var propsWithDifferentNames = me.Bindings.Where(x => ((MemberExpression)((MemberAssignment)x).Expression).Member.Name != x.Member.Name).ToArray();
                if (propsWithDifferentNames.Length > 0)
                    throw new ArgumentException("Template member names must match the target type", nameof(template));
                this.expression = expression;
                break;
            default:
                throw new ArgumentException("Template must be a NewExpression", nameof(template));
        }

        templateMemberExpressions = mex;
        this.template = template;
    }

    private static Expression<Func<TInput, TResult>> CombineExpressions<TInput, TIntermediate, TResult>(
        Expression<Func<TInput, TIntermediate>> exp1,
        Expression<Func<TIntermediate, TResult>> exp2)
    {
        var newParameter = Expression.Parameter(typeof(TInput), exp2.Parameters[0].Name);
        var updatedExp1Body = new RebindParameterVisitor(exp1.Parameters[0], newParameter).Visit(exp1.Body);
        var updatedExp2Body = new RebindParameterVisitor(exp2.Parameters[0], updatedExp1Body!).Visit(exp2.Body);
        return Expression.Lambda<Func<TInput, TResult>>(updatedExp2Body!, newParameter);
    }

    public Spec<TTarget> Adapt<TTarget>(string? newParameterName = null)
    {
        ValidateTypeSignature<TTarget>();

        var adaptedExpression = expression != null
            ? ReplaceParameterType<TTemplate, TTarget, bool>(expression, newParameterName)
            : ReplaceParameterType<TSample, TTarget, bool>(combinedExpression!, newParameterName);

        return Spec<TTarget>.Create(adaptedExpression);
    }

    private void ValidateTypeSignature<TTarget>()
    {
        var tnMembers = typeof(TTarget).GetMembers(BindingFlags.Instance | BindingFlags.Public).ToDictionary(x => x.Name);
        var missingMembers = templateMemberExpressions.Where(expr =>
        {
            var hasMember = tnMembers.TryGetValue(expr.Member.Name, out var member);

            if (!hasMember)
            {
                return false;
            }

            if (member?.GetType() != expr.Member.GetType())
            {
                return false;
            }

            return member switch
            {
                PropertyInfo prop => prop.PropertyType != ((PropertyInfo)expr.Member).PropertyType || !prop.CanRead,
                FieldInfo field => field.FieldType != ((FieldInfo)expr.Member).FieldType,
                _ => false
            };
        }).ToList();

        if (missingMembers.Count > 0)
        {
            throw new InvalidOperationException($"Template members not found in target type: {string.Join(", ", missingMembers.Select(x => x.Member.Name))}");
        }
    }

    private static Expression<Func<TTarget, TResult>> ReplaceParameterType<TSource, TTarget, TResult>(Expression<Func<TSource, TResult>> expr, string? newParameterName)
    {
        var oldParameter = expr.Parameters[0];
        var newParameter = Expression.Parameter(typeof(TTarget), newParameterName ?? oldParameter.Name);
        var visitor = new ParameterTypeVisitor(oldParameter, newParameter);
        var lambdaExpression = (LambdaExpression)visitor.Visit(expr)!;

        //this one can be useful for Proj, not for Spec
        //if (lambdaExpression.ReturnType != typeof(TResult)
        //    && (lambdaExpression.ReturnType.IsSubclassOf(typeof(TResult))
        //        || typeof(TResult).IsAssignableFrom(lambdaExpression.ReturnType)))
        //{
        //    var convertedBody = Expression.Convert(lambdaExpression.Body, typeof(TResult));
        //    return Expression.Lambda<Func<TTarget, TResult>>(convertedBody, newParameter);
        //}

        return (Expression<Func<TTarget, TResult>>)lambdaExpression;

    }

    private class ParameterTypeVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParameter
                ? newParameter
                : base.VisitParameter(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var parameters = node.Parameters.Select(VisitParameter).Cast<ParameterExpression>();
            return Expression.Lambda(Visit(node.Body)!, parameters);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression pex && pex == oldParameter)
            {
                return Expression.Property(Visit(node.Expression)!, node.Member.Name);
            }
            return base.VisitMember(node);
        }
    }
}

public interface ISpecTemplate<TTemplate>
{
    Spec<TTarget> Adapt<TTarget>(string? newParameterName = null);
}