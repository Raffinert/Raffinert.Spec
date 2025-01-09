using Raffinert.Spec;

public static class Queryable
{
    public static IQueryable<T> Where<T>(this IQueryable<T> source, Spec<T> spec)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        return source.Where(spec.GetExpandedExpression());
    }
}