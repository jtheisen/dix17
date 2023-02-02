using System.Diagnostics.CodeAnalysis;

namespace Dix17;

[MetadataFlags("json:type")]
public enum JsonTypeFlags
{
    Object,
    Array,
    Boolean,
    String,
    Number,
    Null
}

[MetadataFlags("fs")]
public enum FileSystemFlags
{
    File,
    Directory
}

[MetadataFlags("s")]
public enum SourceFlags
{
    CanUpdate,
    CanInsert,
    CanRemove
}

public static class MetadataConstants
{
    public const String MdnReflectedClrType = "reflection:clr-type";

    // public static readonly Metadata MdCanInsertOrRemoveEmpty = Dm("empty");
    // public static readonly Metadata MdCanModifyAny = Dm("any");
}

public record MetadataRule(Dix Match, Dix Template)
{
    public MetadataRule(DixMetadata match, DixMetadata template)
        : this(D("match", match), D("template", template))
    {
    }

    public static Dix GetDix(MetadataRule rule) => D("rule",
        D("match", rule.Match),
        D("template", rule.Template)
    );

    public static MetadataRule GetRule(Dix rule)
    {
        return new MetadataRule(
            rule["match"] ?? throw new Exception($"Expected rule to have a match"),
            rule["template"] ?? throw new Exception($"Expected rule to have a template")
        );
    }

    public static Dix GetDix(IEnumerable<MetadataRule> rules)
    {
        return D("md:rules", from r in rules select GetDix(r));
    }

    public static Boolean TryGetRuleSet(Dix dix, [NotNullWhen(true)] out MetadataRuleSet? ruleset)
    {
        ruleset = null;

        if (dix.Name == "md:rules" && dix.Structure is IEnumerable<Dix> structure)
        {
            ruleset = new MetadataRuleSet(structure.Select(GetRule).ToArray());

            return true;
        }

        return false;
    }
}

public interface IMetadataProvider
{
    void AugmentMetadata(Dictionary<String, Dix?> metadata, String prefix);
}

public class MetadataRuleSet : IMetadataProvider, IDixContext
{
    private readonly MetadataRule[] rules;

    public MetadataRule[] Rules => rules;

    public MetadataRuleSet(MetadataRule[] rules)
    {
        this.rules = rules;
    }

    public void AugmentMetadata(Dictionary<String, Dix?> metadata, String prefix)
    {
        foreach (var rule in rules)
        {
            ApplyRuleIfMatch(metadata, rule);
        }
    }

    void ApplyRuleIfMatch(Dictionary<String, Dix?> metadata, MetadataRule rule)
    {
        var doesMatch = rule.Match.Metadata.All(
            m => metadata.TryGetValue(m.Name!, out var v) && v is Dix nnv && nnv.Unstructured == m.Unstructured
        );

        if (doesMatch)
        {
            foreach (var m in rule.Template.Metadata)
            {
                metadata[m.Name!] = m;
            }
        }
    }
}

public class MetadataRulesetBuilder
{
    List<MetadataRule> rules = new List<MetadataRule>();

    public MetadataRulesetBuilder AddRule(MetadataRule rule)
        => Modify(() => rules.Add(rule));

    public MetadataRulesetBuilder AddRule(DixMetadata match, DixMetadata template)
        => Modify(() => rules.Add(new MetadataRule(match, template)));

    public MetadataRuleSet Build()
    {
        return new MetadataRuleSet(rules.ToArray());
    }

    MetadataRulesetBuilder Modify(Action action)
    {
        action();
        return this;
    }

}

