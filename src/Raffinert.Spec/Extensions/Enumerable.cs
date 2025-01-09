using Raffinert.Spec;

public static class Enumerable
{
    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Spec<T> spec)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        return source.Where(spec.IsSatisfiedBy);
    }
}