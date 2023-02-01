namespace Dix17;

public static class AsyncValue<T>
{
    static AsyncLocal<T> Variable = new AsyncLocal<T>();

    public static void Set(T value) => Variable.Value = value;

    public static T? Get() => Variable.Value;
}

public struct AmbientBreakOnError
{
    public Boolean Value { get; set; }

    public static void Enable()
    {
        AsyncValue<AmbientBreakOnError>.Set(new AmbientBreakOnError { Value = true });
    }

    public static Boolean Get() => AsyncValue<AmbientBreakOnError>.Get().Value;
}

public static partial class Extensions
{
    public static Dix Error(this Dix dix, String message)
    {
        var result = D(dix.Name, D("s:error", message)) with { Operation = DixOperation.Error };

        if (AmbientBreakOnError.Get())
        {
            Debugger.Break();
        }

        return result;
    }

    public static Dix ErrorNotImplemented(this Dix dix)
        => dix.Error("not implemented");

    public static Dix ErrorNoName(this Dix dix)
        => dix.Error($"no name");

    public static Dix ErrorInternal(this Dix dix)
        => dix.Error($"internal error");

    public static Dix ErrorMissing(this Dix dix, String? message = null)
        => dix.Error(message ?? $"no such item");

    public static Dix ErrorUnsupportedOperation(this Dix dix)
        => dix.Error($"unsupported operation {dix.Operation}");

    public static Dix WithError(this Dix dix, String message)
        => dix.AddMetadata(D("s:error", message)) with { Operation = DixOperation.Error };
}