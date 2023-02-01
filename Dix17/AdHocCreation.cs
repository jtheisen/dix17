namespace Dix17;

public static class AdHocCreation
{
    public const String DefaultQueryName = "query";

    public static Dix Dq() => D(DefaultQueryName);

    public static Dix Dq(DixContent content) => D(DefaultQueryName, content);


    public static Dix D(String? name, IDixContext? context, String? unstructured, IEnumerable<Dix>? children)
        => new Dix { Operation = DixOperation.Select, Name = name, Content = new CDixContent(unstructured, children.WhereStructure().ToArray(), children.WhereMetadata().ToArray(), context) };

    [DebuggerHidden]
    public static Dix D(String? name, String unstructured, params Dix[] children)
        => D(name, unstructured, children.OfType<Dix>());

    public static Dix D(String? name, String unstructured, IEnumerable<Dix> children, IDixContext? context = null)
        => D(name, context, unstructured, children);

    public static Dix D(String? name, IEnumerable<Dix> children, IDixContext? context = null)
        => D(name, context, null, children);

    public static Dix D(String? name, String? unstructured, IEnumerable<Dix> children, IEnumerable<Dix> moreChildren, IDixContext? context = null)
        => D(name, context, unstructured, children.Concat(moreChildren));

    public static Dix D(String? name, IEnumerable<Dix> children, IEnumerable<Dix> moreChildren, IDixContext? context = null)
        => D(name, context, null, children.Concat(moreChildren));

    public static Dix D(String? name, IEnumerable<Dix> children, params Dix[] moreChildren)
        => D(name, children.Concat(moreChildren).ToArray());

    public static Dix D(String? name, params Dix[] children)
        => D(name, children.OfType<Dix>());

    public static Dix D(String? name, String unstructured, IDixContext? context = null)
        => D(name, context, unstructured, null);

    public static Dix D(String? name, DixContent content, IDixContext? context = null)
        => D(name, context, null, content.Children);


    public static DixContent Dc(IDixContext? context, String? unstructured, IEnumerable<Dix>? children)
        => new DixContent { Content = new CDixContent(unstructured, children.WhereStructure(), children.WhereMetadata(), context) };

    public static DixContent Dc(params String[] children)
        => Dc((IDixContext?)null, null, children.Select(n => D(n)));

    public static DixContent Dc(params Dix[] children)
        => Dc((IDixContext?)null, null, children.OfType<Dix>());


    public static DixMetadata Dm(params Object[] children)
        => new DixMetadata(children.Select(c => GetDixMetadata(c)).SelectMany(c => c));

    static IEnumerable<Dix>? GetDixMetadata(Object child) => child switch
    {
        null => null,
        String s => D(s).Singleton(),
        DixMetadata dm => dm.Metadata,
        Dix d => d.Singleton(),
        _ => throw new Exception($"Unsupported content type {child.GetType()}")
    };
}
