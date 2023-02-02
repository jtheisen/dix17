using Humanizer;
using System.Reflection;

namespace Dix17;

public static partial class Extensions
{
    public static IEnumerable<Dix> GetMetadataForFlagsEnum<E>(this Dix dix)
        where E : struct, Enum
        => dix.GetMetadataWithPrefix(MetadataEnum.GetPrefix<E>());

    public static IEnumerable<E> GetMetadataFlags<E>(this Dix dix)
        where E : struct, Enum
        => dix.GetMetadataForFlagsEnum<E>().Select(d => MetadataEnum.GetEnum<E>(d.Name.AssertMetadataName()));

    public static Boolean TryGetMetadataFlag<E>(this Dix dix, out E flag)
        where E : struct, Enum
    {
        var flags = dix.GetMetadataFlags<E>().ToArray();

        flag = flags.SingleOrDefault($"Expected at most one flag of {MetadataEnum.GetPrefix<E>()}");

        return flags.Length > 0;
    }

}

public abstract class MetadataEnum
{
    public static DixMetadataFlag GetMetadata<E>(E value)
        where E : struct, Enum
    {
        var flags = MetadataEnum<E>.Instance;

        var i = Array.IndexOf(flags.Values, value);

        if (i < 0) throw new Exception();

        return flags.Metadata[i];
    }

    public static E GetEnum<E>(String value)
        where E : struct, Enum
    {
        var flags = MetadataEnum<E>.Instance;

        var i = Array.IndexOf(flags.Names, value);

        if (i < 0) throw new Exception();

        return flags.Values[i];
    }

    public static String GetPrefix<E>() where E : struct, Enum => MetadataEnum<E>.Instance.Prefix;

    protected static String GetPrefix(Type type)
    {
        var a = type.GetCustomAttribute<MetadataFlagsAttribute>();

        if (a is null) throw new Exception();

        return a.Prefix;
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class MetadataFlagsAttribute : FlagsAttribute
{
    public MetadataFlagsAttribute(String prefix)
    {
        Prefix = prefix;
    }

    public String Prefix { get; }
}

public class MetadataEnum<E> : MetadataEnum
    where E : struct, Enum
{
    public static MetadataEnum<E> Instance = new MetadataEnum<E>();

    public String Prefix { get; }
    public E[] Values { get; }
    public String[] Names { get; }
    public DixMetadataFlag[] Metadata { get; }

    public MetadataEnum()
    {
        Prefix = GetPrefix(typeof(E));
        Values = Enum.GetValues<E>();
        Names = Enum.GetNames(typeof(E)).Select(n => $"{Prefix}:{n.Kebaberize()}").ToArray();
        Metadata = Names.Select(n => Dmf(n)).ToArray();
    }

}
