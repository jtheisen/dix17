namespace Dix17;

public static class Utilities
{
    [DebuggerHidden]
    public static IEnumerable<T> Singleton<T>(this T value) => new[] { value };
}
