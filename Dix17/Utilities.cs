namespace Dix17;

public class NoElementException : InvalidOperationException
{
    public NoElementException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class MultipleElementsException : InvalidOperationException
{
    public MultipleElementsException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class InternalErrorException : Exception
{
    public InternalErrorException()
        : base("An internal error occured")
    {
    }

    public InternalErrorException(string message)
        : base($"Internal error: {message}")
    {
    }

    public InternalErrorException(string message, Exception inner)
        : base($"Internal error: {message}", inner)
    {
    }
}

public static class Utilities
{
    [DebuggerHidden]
    public static IEnumerable<T> Singleton<T>(this T value) => new[] { value };

    [DebuggerHidden]
    public static T Return<T>(this object _, T value) => value;

    public static T BreakIf<T>(this T value, bool condition)
    {
        if (condition)
        {
            Debugger.Break();
        }

        return value;
    }

    [DebuggerHidden]
    public static T Single<T>(this IEnumerable<T> source, string error)
    {
        try
        {
            return source.Single();
        }
        catch (InvalidOperationException ex)
        {
            var isNonEmpty = source.Select(x => true).FirstOrDefault();
            if (isNonEmpty) throw new MultipleElementsException(error, ex); else throw new NoElementException(error, ex);
        }
    }

    [DebuggerHidden]
    public static T? SingleOrDefault<T>(this IEnumerable<T> source, string error)
    {
        try
        {
            return source.SingleOrDefault();
        }
        catch (InvalidOperationException ex)
        {
            throw new MultipleElementsException(error, ex);
        }
    }

    [DebuggerHidden]
    public static IEnumerable<Dix>? ConcatNullables(this IEnumerable<Dix>? source, IEnumerable<Dix>? more)
        => source is null ? more : more is null ? source : source.Concat(more);

    public static T Apply<S, T>(this S source, Func<S, T> mapper) => mapper(source);
}
