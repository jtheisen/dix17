using System.Collections;

namespace Dix17;

public class DixValidationException : Exception
{
    public DixValidationException(String message)
        : base(message)
    {
    }
}

[Flags]
public enum DixValidatorFlags
{
    IgnoreExtraUnstructuredIfNullInExpected = 1,
    IgnoreExtraMetadataOnActual = 2
}

public class DixValidator
{
    DixValidatorFlags flags;

    Stack<(Dix actual, Dix expected)> path = new Stack<(Dix, Dix)>();

    void Visit(Dix actual, Dix expected)
    {
        if (actual.Name != expected.Name) Error($"{actual.Name} != {expected.Name}");

        path.Push((actual, expected));

        try
        {
            CheckUnstructured(actual, expected);

            Visit("structure", actual.GetStructure(), expected.GetStructure());

            if (flags.HasFlag(DixValidatorFlags.IgnoreExtraMetadataOnActual))
            {
                Visit(
                    "metadata",
                    expected.GetMetadata(),
                    from ed in expected.GetMetadata()
                    join ad in actual.GetMetadata() on ed.Name equals ad.Name
                    select ad
                );
            }
            else
            {
                Visit("metadata", actual.GetMetadata(), expected.GetMetadata());
            }
        }
        finally
        {
            path.Pop();
        }
    }

    void Visit(String section, IEnumerable<Dix> actual, IEnumerable<Dix> expected)
    {
        var achildren = actual.ToList();
        achildren.Sort(Compare);

        var echildren = expected.ToList();
        echildren.Sort(Compare);

        for (var i = 0; i < achildren.Count; i++)
        {
            var lchild = achildren[i];

            if (echildren.Count <= i) Error($"Item #{i} '{lchild.Name}' of {section} is unexpected");

            var rchild = echildren[i];

            Visit(lchild, rchild);
        }
    }

    void CheckUnstructured(Dix actual, Dix expected)
    {
        if (actual.Unstructured == expected.Unstructured) return;

        if (flags.HasFlag(DixValidatorFlags.IgnoreExtraUnstructuredIfNullInExpected) && expected.Unstructured is null && !actual.IsLeaf()) return;

        Error($"Unstructured content '{actual.Unstructured}' is not '{expected.Unstructured}'");
    }

    void Error(String message)
    {
        var pathString = String.Join("/", path.Select(p => p.expected.Name).Reverse());

        throw new DixValidationException($"Unequal dix, at {pathString}: {message}");
    }

    static Int32 Compare(Dix lhs, Dix rhs)
    {
        return Comparer.Default.Compare(lhs.Name, rhs.Name);
    }

    public static void AssertEqual(Dix expected, Dix actual, DixValidatorFlags flags = default)
        => new DixValidator { flags = flags }.Visit(actual, expected);
}
