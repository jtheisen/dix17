using System.Collections.Immutable;
using System.Linq;

namespace Dix17;

public enum DixOperation
{
    None,
    Update,
    Insert,
    Remove
}

//public struct Dix
//{
//    public DixOperation Operation { get; set; }

//    public String? Name { get; set; }


//}

public class CDix : IDix
{
    public DixOperation Operation { get; }

    public String? Name { get; }

    public String? Unstructured { get; }

    public IEnumerable<IDix> Children { get; }

    public CDix(String? name, String? unstructured, IEnumerable<IDix>? children)
    {
        Name = name;
        Unstructured = unstructured;
        Children = children ?? Enumerable.Empty<IDix>();
    }

    public CDix(String? name, IEnumerable<IDix> children)
    {
        Name = name;
        Children = children ?? Enumerable.Empty<IDix>();
    }

    public CDix(String? name, String unstructured)
    {
        Name = name;
        Unstructured = unstructured;
        Children = Enumerable.Empty<IDix>();
    }
}



public interface IDix
{
    String? Name { get; }

    String? Unstructured { get; }

    IEnumerable<IDix> Children { get; }
}


public class InheritedDix : IDix
{
    private readonly IDix baseDix;
    private readonly String? name;

    public InheritedDix(IDix baseDix, String? name)
    {
        this.baseDix = baseDix;
        this.name = name ?? baseDix.Name;
    }

    public String? Name => name;

    public String? Unstructured => baseDix.Unstructured;

    public IEnumerable<IDix> Children => baseDix.Children;
}

public interface IStructureAwareness
{
    String Destructurize(IDix structure);

    IDix Structurize(String unstructured);
}


public static class Extensions
{
    public static Boolean IsMetadataName(this String name)
        => name.Contains(':');

    public static IEnumerable<IDix> GetStructure(this IDix dix)
        => dix.Children.Where(d => !d.Name?.IsMetadataName() ?? true);

    public static IEnumerable<IDix> GetMetadata(this IDix dix)
        => dix.Children.Where(d => d.Name?.IsMetadataName() ?? false);

    public static Boolean IsLeaf(this IDix dix)
        => dix.Children.FirstOrDefault() is null ? true : false;

    public static IDix? GetStructure(this IDix dix, String name)
        => dix.GetStructure().SingleOrDefault(d => d.Name == name);

    public static IDix? GetMetadata(this IDix dix, String name)
        => dix.GetMetadata().SingleOrDefault(d => d.Name == name);

    public static Boolean HasMetadataFlag(this IDix dix, String name)
        => dix.GetMetadata(name) is not null;

    public static Boolean HasMetadata(this IDix dix, IDix metadata)
        => dix.GetMetadata(metadata.Name!) is IDix d && d.Unstructured == metadata.Unstructured;

    public static Boolean HasMetadataValue(this IDix dix, String name, String value)
        => dix.GetMetadata(name) is IDix d && d.Unstructured == value;

    public static IDix WithName(this IDix dix, String name)
        => new CDix(name, dix.Unstructured, dix.Children);

    public static IDix WithStructure(this IDix dix, IEnumerable<IDix> structure)
        => new CDix(dix.Name, dix.Unstructured, dix.GetMetadata().Concat(structure));

    public static IDix WithOperation(this IDix dix, DixOperation operation)
        => new CDix(dix.Name, dix.Unstructured, dix.Children, operation);

    public static IDix AddMetadata(this IDix dix, IEnumerable<IDix> metadata)
        => new CDix(dix.Name, dix.Unstructured, dix.Children.Concat(metadata));

    public static IDix AddMetadata(this IDix dix, IDix metadata)
        => new CDix(dix.Name, dix.Unstructured, dix.Children.Concat(new[] { metadata }));

    public static String Format(this IDix dix)
        => Formatter.Format(dix);
}


/* we need
 * - some ad-hoc Dix creation
 * - create an IDix from the Dix
 */