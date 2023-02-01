namespace Dix17;

public abstract class DixVisitor
{
    public virtual Dix Visit(Dix dix)
    {
        VisitBefore(dix);

        foreach (var child in dix.Metadata)
        {
            Visit(child);
        }

        if (dix.Unstructured is String unstructured)
        {
            VisitUnstructured(dix, unstructured);
        }
        else if (dix.Structure is IEnumerable<Dix> structure)
        {
            foreach (var c in structure)
            {
                Visit(c);
            }
        }

        return dix;
    }

    public virtual void VisitUnstructured(Dix dix, String unstructured) { }

    public virtual void VisitBefore(Dix dix) { }
}

public class ConcreteDixVisitor : DixVisitor
{
    private readonly Action<Dix> action;

    public ConcreteDixVisitor(Action<Dix> action)
    {
        this.action = action;
    }

    public override void VisitBefore(Dix dix) => action(dix);
}

public static partial class Extensions
{
    public static Dix Visit(this Dix dix, Action<Dix> visitor)
        => new ConcreteDixVisitor(visitor).Visit(dix);
}