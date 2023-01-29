namespace Dix17;

public static class AdHocCreation
{
    public static IDix D(String? name, String unstructured, params IDix[] children)
        => new CDix(name, unstructured, children);

    public static IDix D(String? name, String unstructured, IEnumerable<IDix> children)
        => new CDix(name, unstructured, children);

    public static IDix D(String? name, IEnumerable<IDix> children)
        => new CDix(name, children);

    public static IDix D(String? name, IEnumerable<IDix> children, params IDix[] moreChildren)
        => new CDix(name, children.Concat(moreChildren));

    public static IDix D(String? name, params IDix[] children)
        => new CDix(name, children);

    public static IDix D(String? name, String unstructured)
        => new CDix(name, unstructured);

}
