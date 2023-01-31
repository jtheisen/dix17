using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dix17;

public enum DixOperation
{
    Invalid,
    Select,
    Update,
    Insert,
    Remove,
    Error
}

[DebuggerDisplay("{ToString()}")]
public struct Dix
{
    public DixOperation Operation { get; set; }

    public String? Name { get; set; }

    public CDixContent? Content { get; set; }

    public Boolean IsNaD => Operation == DixOperation.Invalid;

    public Boolean HasEmptyContent => Content?.IsEmptyContent ?? false;

    public Boolean HasNilContent => Content?.IsNilContent ?? true;

    public String? Unstructured => Content?.Unstructured;

    public IDixContext? Context => Content?.Context;

    public Dix? this[String name] => Structure?.FirstOrDefault(d => d.Name == name).Nullify();

    public IEnumerable<Dix>? Structure => Content?.Structure;
    public IEnumerable<Dix> Metadata => Content?.Metadata ?? Enumerable.Empty<Dix>();

    public Dix? GetMetadata(String name) => Content?.GetMetadata(name);

    public Dix WithName(String? name) => this with { Name = name };
    public Dix WithoutOperation() => this with { Operation = DixOperation.Select };
    public Dix WithError() => this with { Operation = DixOperation.Error };

    public Dix? Nullify() => IsNaD ? null : this;

    public static Dix operator ~(Dix dix) => dix with { Operation = DixOperation.Update };
    //public static Dix operator !(Dix dix) => dix with { Operation = DixOperation.Error };
    public static Dix operator +(Dix dix) => dix with { Operation = DixOperation.Insert };
    public static Dix operator -(Dix dix) => dix with { Operation = DixOperation.Remove };

    public String Formatted => SimpleFormatter.Format(this);

    public override String ToString()
    {
        var prefix = SimpleFormatter.GetOperationCharacter(Operation).ToString().Trim();

        var name = Name ?? "_";

        var ucontent = Unstructured is not null ? "=" + Unstructured : "";

        var n = 3;

        var structure = this.GetStructure().Take(n).ToArray();

        var scontent = $"[{String.Join(", ", structure.Take(n - 1).Select(d => d.Name))}]";

        return $"{prefix}{name}{ucontent}{scontent}";
    }
}

public struct DixContent
{
    public CDixContent? Content { get; set; }

    public Boolean HasEmptyContent => Content?.IsEmptyContent ?? false;

    public Boolean HasNilContent => Content?.IsNilContent ?? true;

    public String? Unstructured => Content?.Unstructured;

    public IEnumerable<Dix>? Children => Content?.Structure.ConcatNullables(Content?.Metadata);

    public IDixContext? Context => Content?.Context;

    public Dix? this[String name] => Structure.FirstOrDefault(d => d.Name == name).Nullify();

    public IEnumerable<Dix> Structure => Content?.Structure ?? Enumerable.Empty<Dix>();
    public IEnumerable<Dix> Metadata => Content?.Metadata ?? Enumerable.Empty<Dix>();
}

public struct DixMetadata
{
    public IEnumerable<Dix>? Metadata { get; set; }

    public DixMetadata(IEnumerable<Dix>? metadata)
    {
        Metadata = metadata;
    }

    public static DixMetadata operator +(DixMetadata lhs, DixMetadata rhs)
        => new DixMetadata(lhs.Metadata.ConcatNullables(rhs.Metadata));
}

public interface IDixContext
{
    void AugmentMetadata(Dictionary<String, Dix?> metadata, String prefix);
}

public interface IDixContent
{
    String? Unstructured { get; }

    IEnumerable<Dix>? Structure { get; }

    IEnumerable<Dix>? Metadata { get; }

    Dix? GetMetadata(String name);

    Boolean IsEmptyContent { get; }

    Boolean IsNilContent { get; }
}

public class CDixContent : IDixContent
{
    public String? Unstructured { get; }

    public IEnumerable<Dix>? Structure { get; }

    public IEnumerable<Dix>? Metadata { get; }

    public IDixContext? Context { get; }

    public Boolean HasStructure => Structure?.Any() ?? false;

    public Boolean IsNilContent => Unstructured is null && !HasStructure;

    public Boolean IsEmptyContent => Unstructured == "" || (Unstructured is null && !HasStructure);

    public Dix? GetMetadata(String name)
    {
        if (metadata is null)
        {
            if (Metadata is not null)
            {
                metadata = Metadata.ToDictionary(d => d.Name!, d => (Dix?)d);
            }
            else
            {
                metadata = new Dictionary<String, Dix?>();
            }
        }

        if (!metadata.TryGetValue(name, out var result) && Context is not null)
        {
            Context.AugmentMetadata(metadata, name);

            metadata.TryGetValue(name, out result);

            metadata[name] = result;
        }

        return result;
    }


    Dictionary<String, Dix?>? metadata = null;

    public CDixContent(String? unstructured, IEnumerable<Dix>? structured, IEnumerable<Dix>? metadata, IDixContext? context)
    {
        Unstructured = unstructured;
        Structure = structured;
        Metadata = metadata;
        Context = context;

        if (Unstructured is not null && HasStructure) throw new Exception($"Can't have structured and unstructured data at once");
    }
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
        => dix.Structure ?? Enumerable.Empty<Dix>();

    public static IEnumerable<Dix> GetMetadata(this Dix dix)
        => dix.Metadata;

    public static Boolean IsLeaf(this Dix dix)
        => !dix.GetStructure().Any();

    public static Dix GetStructure(this Dix dix, String name)
        => dix.GetStructure().SingleOrDefault(d => d.Name == name);

    public static String? GetMetadataValue(this Dix dix, String name)
        => dix.GetMetadata().SingleOrDefault(d => d.Name == name).Unstructured;

    public static Boolean HasMetadataFlag(this Dix dix, String name)
        => dix.GetMetadata(name) is not null;

    public static Boolean HasMetadata(this Dix dix, Dix metadata)
        => dix.GetMetadata(metadata.Name!) is Dix d && d.Unstructured == metadata.Unstructured;

    public static Boolean HasMetadataValue(this Dix dix, String name, String value)
        => dix.GetMetadata(name) is Dix d && d.Unstructured == value;

    public static Dix WithStructure(this Dix dix, IEnumerable<Dix> structure)
        => dix with { Content = new CDixContent(dix.Unstructured, structure, dix.Metadata, dix.Context) };

    public static Dix AddStructure(this Dix dix, IEnumerable<Dix> structure)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure?.ConcatNullables(structure), dix.Metadata, dix.Context) };

    public static Dix AddStructure(this Dix dix, Dix structure)
        => dix.AddStructure(structure.Singleton());

    public static Dix WithContent(this Dix dix, Dix content)
        => dix with { Content = content.Content };

    public static Dix WithContext(this Dix dix, IDixContext context)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure, dix.Metadata, context) };

    public static Dix WithMetadata(this Dix dix, DixMetadata metadata = default)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure, metadata.Metadata, dix.Context) };

    public static Dix AddMetadata(this Dix dix, DixContent content)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure, dix.Metadata.ConcatNullables(content.Metadata), dix.Context) };

    public static Dix AddMetadata(this Dix dix, DixMetadata metadata)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure, dix.Metadata.ConcatNullables(metadata.Metadata), dix.Context) };

    public static Dix AddMetadata(this Dix dix, IEnumerable<Dix>? metadata)
        => dix with { Content = new CDixContent(dix.Unstructured, dix.Structure, dix.Metadata.ConcatNullables(metadata), dix.Context) };

    public static Dix AddMetadata(this Dix dix, Dix metadata)
        => dix.AddMetadata(metadata.Singleton());

    public static Dix Map(this Dix dix, Func<Dix, Dix> mapper)
        => dix.Structure is IEnumerable<Dix> structure ? dix.WithStructure(structure.Select(mapper)) : dix;

    public static Dix MapRecursively(this Dix dix, Func<Dix, Dix> mapper)
        => dix.Structure is IEnumerable<Dix> structure ? dix.WithStructure(structure.Select(mapper).Select(d => d.MapRecursively(mapper))) : dix;

    public static String Format(this Dix dix)
        => SimpleFormatter.Format(dix);

    public static String FormatCSharp(this Dix dix)
        => CSharpFormatter.Format(dix);

    public static Dix WithError(this Dix dix, String message)
        => dix.AddMetadata(D("s:error", message)) with { Operation = DixOperation.Error };

    public static IEnumerable<Dix> WhereMetadata(this IEnumerable<Dix>? source)
        => source?.Where(d => d.Name?.IsMetadataName() ?? false) ?? Enumerable.Empty<Dix>();

    public static IEnumerable<Dix> WhereStructure(this IEnumerable<Dix>? source)
        => source?.Where(d => !d.Name?.IsMetadataName() ?? true) ?? Enumerable.Empty<Dix>();


    public static Dix RecursivelyRemoveMetadata(this Dix dix)
        => dix.WithMetadata().Map(RecursivelyRemoveMetadata);
}


/* we need
 * - some ad-hoc Dix creation
 * - create an IDix from the Dix
 */