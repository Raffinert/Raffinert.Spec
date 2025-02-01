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

public class SpecTemplate<TSample, TTemplate>
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
            case MemberExpression mex0:
                mex = [mex0];
                combinedExpression = CombineExpressions(template, expression);
                break;
            default:
                throw new ArgumentException("Template must be either a NewExpression or MemberExpression", nameof(template));
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

    public Spec<TN> Adapt<TN>(string? newParameterName = null)
    {
        ValidateTypeSignature<TN>();

        var adaptedExpression = expression != null 
            ? Convert<TTemplate, TN, bool>(expression, newParameterName) 
            : Convert<TSample, TN, bool>(combinedExpression!, newParameterName);

        return Spec<TN>.Create(adaptedExpression);
    }

    private void ValidateTypeSignature<TN>()
    {
        var tnMembers = typeof(TN).GetMembers(BindingFlags.Instance | BindingFlags.Public).ToDictionary(x => x.Name);
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

    private static Expression<Func<TN, TR>> Convert<TT, TN, TR>(Expression<Func<TT, TR>> root, string? newParameterName)
    {
        var visitor = new ParameterTypeVisitor(root.Parameters[0], Expression.Parameter(typeof(TN), newParameterName ?? root.Parameters[0].Name));
        var expression = (Expression<Func<TN, TR>>)visitor.Visit(root)!;
        return expression;
    }

    public class ParameterTypeVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
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