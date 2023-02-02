namespace Dix17;

public static class AdHocCreation
{
    public const String DefaultQueryName = "query";

    public static Dix Dq() => D(DefaultQueryName);

    public static Dix Dq(DixStructure structure) => D(DefaultQueryName, structure);


    public static Dix D(String? name)
        => new Dix { Operation = DixOperation.Select, Name = name };

    public static Dix D(String? name, DixContent content)
        => new Dix { Operation = DixOperation.Select, Name = name, Content = new CDixContent(content.Unstructured, content.Structure, content.Metadata, content.Context) };

    public static Dix D(String? name, DixMetadata metadata, IDixContext? context = null)
        => new Dix { Operation = DixOperation.Select, Name = name, Content = new CDixContent(null, null, metadata.Metadata, context) };

    public static Dix D(String? name, String unstructured, DixMetadata metadata = default, IDixContext? context = null)
        => new Dix { Operation = DixOperation.Select, Name = name, Content = new CDixContent(unstructured, null, metadata.Metadata, context) };

    public static Dix D(String? name, IEnumerable<Dix> structure, DixMetadata metadata = default, IDixContext? context = null)
        => new Dix { Operation = DixOperation.Select, Name = name, Content = new CDixContent(null, structure, metadata.Metadata, context) };

    public static Dix D(String? name, DixMetadata metadata, params Dix[] structured)
        => D(name, structured, metadata, null);

    public static Dix D(String? name, params Dix[] structured)
        => D(name, structured, default, null);

    public static Dix D(String? name, DixStructure structure, DixMetadata metadata = default, IDixContext? context = null)
        => D(name, structure.Structure, metadata, context);


    public static DixMetadata Dm(String? name)
        => new DixMetadata(D(name.AssertMetadataName()).Singleton());

    public static DixMetadataFlag Dmf(String? name)
        => new DixMetadataFlag(name.AssertMetadataName());

    public static DixMetadataFlag Dmf<E>(E flag)
        where E : struct, Enum
        => MetadataEnum.GetMetadata(flag);

    public static DixContent Dc(IEnumerable<Dix> content)
        => new DixContent { Content = new CDixContent(null, content.WhereStructure(), content.WhereMetadata(), null) };

    public static DixMetadata Dm(String? name, String unstructured)
        => new DixMetadata(D(name.AssertMetadataName(), unstructured).Singleton());

    public static DixMetadata Dm(params DixMetadata[] metadata)
        => new DixMetadata(metadata.SelectMany(m => m.Metadata ?? Enumerable.Empty<Dix>()));

    public static DixMetadata Dmc(params Object[] children)
        => new DixMetadata(children.SelectMany(CollectMetadata).SelectMany(m => m.Metadata ?? Enumerable.Empty<Dix>()).ToArray());

    //public static DixStructure Ds(params Object[] children)
    //    => new DixStructure(children.Select(c => CollectDixes(c, false)).SelectMany(c => c));


    static IEnumerable<DixMetadata> CollectMetadata(Object? target)
    {
        if (target is null) yield break;

        if (target is String s)
        {
            yield return Dm(s.AssertMetadataName());
        }
        else if (target is DixMetadata dm)
        {
            yield return dm;
        }
        else
        {
            throw new Exception($"Unsupported content type {target.GetType()}");
        }
    }
}
