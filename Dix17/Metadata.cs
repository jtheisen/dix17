using System.Diagnostics.CodeAnalysis;
using static Dix17.AdHocCreation;

namespace Dix17;

public static class MetadataConstants
{
    public const String JsonType = "x:json-type";

    public const String JsonTypeObject = "object";
    public const String JsonTypeArray = "array";
    public const String JsonTypeBoolean = "boolean";
    public const String JsonTypeString = "string";
    public const String JsonTypeNumber = "number";
    public const String JsonTypeNull = "null";

    public const String ReflectedClrType = "reflection:clr-type";
    public const String ReflectedType = "reflection:type";

    public const String ReflectedTypeObject = "object";
    public const String ReflectedTypeEnumerable = "enumerable";
    public const String ReflectedTypeBoolean = "boolean";
    public const String ReflectedTypeString = "string";
    public const String ReflectedTypeNumber = "number";
    public const String ReflectedTypeNull = "null";

    public const String FileSystemEntry = "fs:entry";
    public const String FileSystemEntryFile = "file";
    public const String FileSystemEntryDirectory = "directory";
}

public static class MetadataForSources
{
    public const String MdnCanUpdate = "s:can-update";
    public const String MdnCanInsert = "s:can-insert";
    public const String MdnCanRemove = "s:can-remove";

    public const String MdvCanInsertOrRemoveEmpty = "empty";
    public const String MdvCanModifyAny = "any";

    public const String MdnOneOperationOnly = "s:one-operation-only";

    

    /* Various answers to a query at a specific non-root stage:
     * 
     * - ask me again just for this (especially when modifying)
     * - actual error
     * - ok
     * 
     * When declaring capabilities, it can say
     * 
     * - you can select/insert/remove/update
     *   - you musn't provide subelements
     *   - you must provide sublements of 1 level
     * - you must (not) do one operation at a time (given at the root)
     */
}

/* We need metadata
 * - to be retrievable on-demand for a specific key
 * - to be expanded to the object for a specific namespace before handing it to a source (later)
 *   - for example, inferred json metadata from a given CRL representation needs to be handed to the structurizer
 *   - for example, inferred fs metadata needs to be handed to the file system source
 * 
 * Additional metadata comes
 * - directly from context, inherited down a chain later to be established
 * - inferred from rules which also come from context
 */

public record MetadataRule(Dix Match, Dix Template)
{
    public MetadataRule(DixContent match, DixContent template)
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

        if (dix.Name == "md:rules")
        {
            ruleset = new MetadataRuleSet(dix.Structure.Select(GetRule).ToArray());

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

    public MetadataRulesetBuilder AddRule(DixContent match, DixContent template)
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
