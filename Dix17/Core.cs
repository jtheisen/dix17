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

[DebuggerDisplay("{ToString()}")]
public struct Dix
{
    public DixOperation Operation { get; set; }

    public String? Name { get; set; }

    public IDixContent? Content { get; set; }

    public Boolean IsEmpty => Content is null;

    public String? Unstructured => Content?.Unstructured;
    public IEnumerable<Dix> Children => Content?.Children ?? Enumerable.Empty<Dix>();

    public Dix WithName(String? name) => this with { Name = name };
    public Dix WithoutOperation() => this with { Operation = DixOperation.None };

    public static Dix operator ~(Dix dix) => dix with { Operation = DixOperation.None };
    public static Dix operator !(Dix dix) => dix with { Operation = DixOperation.Update };
    public static Dix operator +(Dix dix) => dix with { Operation = DixOperation.Insert };
    public static Dix operator -(Dix dix) => dix with { Operation = DixOperation.Remove };


    public override String ToString()
    {
        var name = Name ?? "_";

        var ucontent = Unstructured is not null ? "=" + Unstructured : "";

        var n = 3;

        var structure = this.GetStructure().Take(n).ToArray();

        var scontent = $"[{String.Join(", ", structure.Take(n - 1).Select(d => d.Name))}]";

        return $"{name}{ucontent}{scontent}";
    }
}

public class CDixContent : IDixContent
{
    public String? Unstructured { get; }

    public IEnumerable<Dix> Children { get; }

    public CDixContent(String? unstructured, IEnumerable<Dix>? children)
    {
        Unstructured = unstructured;
        Children = children ?? Enumerable.Empty<Dix>();
    }

    public CDixContent(IEnumerable<Dix> children)
    {
        Children = children ?? Enumerable.Empty<Dix>();
    }

    public CDixContent(String unstructured)
    {
        Unstructured = unstructured;
        Children = Enumerable.Empty<Dix>();
    }
}



public interface IDixContent
{
    String? Unstructured { get; }

    IEnumerable<Dix> Children { get; }
}


public class InheritedDixContent : IDixContent
{
    private readonly IDixContent baseDixContent;

    public InheritedDixContent(IDixContent baseDixContent)
    {
        this.baseDixContent = baseDixContent;
    }

    public String? Unstructured => baseDixContent.Unstructured;

    public IEnumerable<Dix> Children => baseDixContent.Children;
}

public interface IStructureAwareness
{
    String Destructurize(Dix structure);

    Dix Structurize(String unstructured);
}


public static partial class Extensions
{
    public static Boolean IsMetadataName(this String name)
        => name.Contains(':');

    public static IEnumerable<Dix> GetStructure(this Dix dix)
        => dix.Children.Where(d => !d.Name?.IsMetadataName() ?? true);

    public static IEnumerable<Dix> GetMetadata(this Dix dix)
        => dix.Children.Where(d => d.Name?.IsMetadataName() ?? false);

    public static Boolean IsLeaf(this Dix dix)
        => dix.GetStructure().FirstOrDefault().IsEmpty ? true : false;

    public static Dix? GetStructure(this Dix dix, String name)
        => dix.GetStructure().SingleOrDefault(d => d.Name == name);

    public static Dix? GetMetadata(this Dix dix, String name)
        => dix.GetMetadata().SingleOrDefault(d => d.Name == name);

    public static String? GetMetadataValue(this Dix dix, String name)
        => dix.GetMetadata().SingleOrDefault(d => d.Name == name).Unstructured;

    public static Boolean HasMetadataFlag(this Dix dix, String name)
        => dix.GetMetadata(name) is not null;

    public static Boolean HasMetadata(this Dix dix, Dix metadata)
        => dix.GetMetadata(metadata.Name!) is Dix d && d.Unstructured == metadata.Unstructured;

    public static Boolean HasMetadataValue(this Dix dix, String name, String value)
        => dix.GetMetadata(name) is Dix d && d.Unstructured == value;

    public static Dix WithStructure(this Dix dix, IEnumerable<Dix> structure)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.GetStructure().Concat(structure)) };

    public static Dix AddStructure(this Dix dix, Dix structure)
        => dix.WithStructure(structure.Singleton());

    public static Dix WithContent(this Dix dix, Dix content)
        => dix with { Content = content.Content };

    public static Dix AddMetadata(this Dix dix, IEnumerable<Dix> metadata)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Children.Concat(metadata)) };

    public static Dix AddMetadata(this Dix dix, Dix metadata)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Children.Concat(new[] { metadata })) };

    public static String Format(this Dix dix)
        => SimpleFormatter.Format(dix);

    public static String FormatCSharp(this Dix dix)
        => CSharpFormatter.Format(dix);
}


/* we need
 * - some ad-hoc Dix creation
 * - create an IDix from the Dix
 */