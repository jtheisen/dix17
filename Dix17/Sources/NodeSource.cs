namespace Dix17.Sources;

public interface INode<Node>
    where Node : INode<Node>
{
    String? Name { get; }

    DixMetadata Metadata { get; }

    String? Unstructured { get; }

    IEnumerable<Node>? Structured { get; }
}

public abstract class NodeSource<Node> : ISource
    where Node : class, INode<Node>
{
    public Dix Query(Dix dix)
    {
        if (dix.IsLeaf() && dix.Operation == DixOperation.Select)
        {
            return GetTeaser(dix, dix.Name!, GetRoot()).AddMetadata(GetTopLevelMetadata());
        }
        else
        {
            return ProcessRoot(dix, GetRoot());
        }
    }

    Dix ProcessRoot(Dix dix, Node rootNode) => dix.Operation switch
    {
        DixOperation.Select => D(dix.Name, GetContent(dix, rootNode)),
        DixOperation.Update => Update(dix, null, rootNode),
        _ => dix.ErrorUnsupportedOperation()
    };

    Dix Process(Dix dix, Node? parentTarget, Node? target) => dix.Operation switch
    {
        DixOperation.Select => Select(dix, target),
        DixOperation.Update => target is Node n ? Update(dix, parentTarget, n) : dix.Error($"No node to update for {dix.Name}"),
        DixOperation.Insert => InsertInternal(dix, parentTarget, target),
        DixOperation.Remove => RemoveInternal(dix, parentTarget, target),
        _ => dix.ErrorUnsupportedOperation()
    };

    Dix Select(Dix dix, Node? target)
    {
        if (target is null)
        {
            return D(dix.Name).WithError("no such entry");
        }
        else if (dix.IsLeaf())
        {
            if (dix.Name is null) return dix.ErrorNoName();

            if (target is null) return dix.Error($"no node");

            return GetTeaser(dix, dix.Name, target);
        }
        else
        {
            if (target is null) return dix.Error($"no node");

            return D(dix.Name, GetContent(dix, target));
        }
    }

    DixContent GetContent(Dix dix, Node target)
    {
        var content =
            from d in dix.Structure
            let name = d.Name
            let c = name is not null ? GetChild(target, name) : null
            select name is not null ? Process(d, target, c) : d.ErrorNoName();

        return Dc(content.ToArray());
    }

    protected virtual DixContent GetTopLevelMetadata() => default;

    protected abstract Node GetRoot();

    protected abstract Node? GetChild(Node parent, String name);

    protected virtual Dix GetTeaser(Dix dix, String name, Node node)
    {
        if (node.Unstructured is String unstructured)
        {
            return WithMetadata(D(name, unstructured), node);
        }
        else if (node.Structured is IEnumerable<Node> children)
        {
            return WithMetadata(D(name, from c in children select WithMetadata(D(c.Name), c)), node);
        }
        else
        {
            return WithMetadata(D(name), node);
        }
    }

    protected virtual Dix Update(Dix dix, Node? parentTarget, Node target)
    {
        if (dix.Unstructured is String unstructured)
        {
            if (!dix.IsLeaf()) dix.Error($"Updates can't contain both structured and unstructured data");

            return UpdateUnstructured(dix, target, unstructured);
        }
        else
        {
            return UpdateStructured(dix, target);
        }
    }

    Dix RemoveInternal(Dix dix, Node? parentTarget, Node? target)
    {
        if (parentTarget is null) return dix.Error($"Can't remove the root");

        if (target is null) return dix.Error($"No node to remove under {dix.Name} in {parentTarget}");

        return Remove(dix, parentTarget, target);
    }

    Dix InsertInternal(Dix dix, Node? parentTarget, Node? target)
    {
        if (target is not null) return dix.Error($"Already have a node under {dix.Name}: {target}");

        if (parentTarget is null) return dix.Error($"Can't insert the root");

        return Insert(dix, parentTarget);
    }

    protected Dix WithMetadata(Dix dix, Node node)
        => dix.AddMetadata(node.Metadata);

    protected virtual Dix Insert(Dix dix, Node parentTarget) => dix.ErrorNotImplemented();
    protected virtual Dix Remove(Dix dix, Node parentTarget, Node target) => dix.ErrorNotImplemented();

    protected virtual Dix UpdateStructured(Dix dix, Node target) => dix.ErrorNotImplemented();
    protected virtual Dix UpdateUnstructured(Dix dix, Node target, String unstructured) => dix.ErrorNotImplemented();
}
