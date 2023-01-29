namespace Dix17;

public static class AdHocCreation
{
    public static Dix D(String? name, String unstructured, IEnumerable<Dix> children)
        => new Dix { Name = name, Content = new CDixContent(unstructured, children) };

    public static Dix D(String? name, IEnumerable<Dix> children)
        => new Dix { Name = name, Content = new CDixContent(children) };

    public static Dix D(String? name, String unstructured, params Dix[] children)
        => D(name, unstructured, children.OfType<Dix>());

    public static Dix D(String? name, IEnumerable<Dix> children, params Dix[] moreChildren)
        => D(name, children.Concat(moreChildren));

    public static Dix D(String? name, params Dix[] children)
        => D(name, children.OfType<Dix>());

    public static Dix D(String? name, String unstructured)
        => D(name, unstructured, Enumerable.Empty<Dix>());

}
