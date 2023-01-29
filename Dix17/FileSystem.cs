namespace Dix17;

public abstract class AbstractSource<Node> : ISource
    where Node : class
{
    public Dix Query(Dix dix)
    {
        return Process(dix, null, GetRoot());
    }

    Dix Process(Dix dix, Node? parentTarget, Node target) => dix.Operation switch
    {
        DixOperation.None => dix.IsLeaf()
            ? GetTeaser(dix.Name ?? throw new Exception($"No name"), target)
            : D(dix.Name,
                from d in dix.GetStructure()
                let c = GetChild(target, d.Name ?? throw new Exception($"No name")) ?? throw new Exception()
                select Process(d, target, c)
            ),
        DixOperation.Update => Update(dix, target),
        DixOperation.Insert => Insert(dix, target),
        DixOperation.Remove => Remove(dix, target),
        _ => throw new Exception($"Unsupported operation {dix.Operation}")
    };

    protected abstract Node GetRoot();

    protected abstract Node? GetChild(Node parent, String name);

    protected abstract Dix GetTeaser(String name, Node node);

    protected virtual Dix Update(Dix dix, Node target)
    {
        if (dix.Unstructured is String unstructured)
        {
            if (!dix.IsLeaf()) throw new Exception($"Updates can't contain both structured and unstructured data");

            return UpdateUnstructured(target, unstructured);
        }
        else
        {
            return UpdateStructured(dix, target);
        }
    }

    protected virtual Boolean CanUpdate(Node target) => false;
    protected virtual Boolean CanRemove(Node parentNode, Node target) => false;
    protected virtual Boolean CanInsert(Node parentNode, Node target) => false;

    protected virtual Dix Insert(Dix dix, Node target) => throw new NotImplementedException();
    protected virtual Dix Remove(Dix dix, Node target) => throw new NotImplementedException();

    protected virtual Dix UpdateStructured(Dix dix, Node parentTarget) => throw new NotImplementedException();
    protected virtual Dix UpdateUnstructured(Node parentTarget, String unstructured) => throw new NotImplementedException();
}

public class MockupFileSystemSource : AbstractSource<MockupFileSystemSource.Node>
{
    DirectoryNode root = new DirectoryNode();

    protected override Node? GetChild(Node parent, String name) => parent switch
    {
        DirectoryNode d => d.Children.GetValueOrDefault(name),
        _ => null
    };

    protected override Node GetRoot() => root;

    protected override Boolean CanUpdate(Node target) => target is FileNode;

    protected override Dix GetTeaser(String name, Node node) => node switch
    {
        FileNode f => D(name, f.Content),
        DirectoryNode d => D(name, from e in d.Children select D(e.Key, D(Metadata.FileSystemEntry, e.Value.EntryTypeMetadataValue))),
        _ => throw new Exception()
    };

    public abstract class Node
    {
        public abstract String EntryTypeMetadataValue { get; }
    }

    public class DirectoryNode : Node
    {
        public override String EntryTypeMetadataValue => Metadata.FileSystemEntryDirectory;

        public Dictionary<String, Node> Children { get;  } = new Dictionary<String, Node>();
    }

    public class FileNode : Node
    {
        public override String EntryTypeMetadataValue => Metadata.FileSystemEntryFile;

        public String Content { get; set; } = "";
    }
}
