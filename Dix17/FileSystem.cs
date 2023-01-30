using System.IO;

namespace Dix17;

public abstract class AbstractSource<Node> : ISource
    where Node : class
{
    public Dix Query(Dix dix)
    {
        return Process(dix, null, GetRoot());
    }

    Dix Process(Dix dix, Node? parentTarget, Node? target) => dix.Operation switch
    {
        DixOperation.None => dix.IsLeaf()
            ? GetTeaser(dix.Name ?? throw new Exception($"No name"), target ?? throw new Exception($"No node for {dix.Name}"))
            : D(dix.Name,
                from d in dix.GetStructure()
                let c = GetChild(target ?? throw new Exception($"No node for {dix.Name}"), d.Name ?? throw new Exception($"No name"))
                select Process(d, target, c)
            ),
        DixOperation.Update => Update(dix, parentTarget, target ?? throw new Exception($"No node to update for {dix.Name}")),
        DixOperation.Insert => InsertInternal(dix, parentTarget, target),
        DixOperation.Remove => RemoveInternal(dix, parentTarget, target),
        _ => throw new Exception($"Unsupported operation {dix.Operation}")
    };

    protected abstract Node GetRoot();

    protected abstract Node? GetChild(Node parent, String name);

    protected abstract Dix GetTeaser(String name, Node node);

    protected virtual Dix Update(Dix dix, Node? parentTarget, Node target)
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

    Dix RemoveInternal(Dix dix, Node? parentTarget, Node? target)
    {
        if (target is null) throw new Exception($"No node to remove under {dix.Name}");

        if (parentTarget is null) throw new Exception($"Can't remove the root");

        return Remove(dix, parentTarget, target);
    }

    Dix InsertInternal(Dix dix, Node? parentTarget, Node? target)
    {
        if (target is not null) throw new Exception($"Already have a node under {dix.Name}");

        if (parentTarget is null) throw new Exception($"Can't insert the root");

        return Insert(dix, parentTarget);
    }

    protected virtual Boolean CanUpdate(Node target) => false;
    protected virtual Boolean CanRemove(Node parentNode, Node target) => false;
    protected virtual Boolean CanInsert(Node parentNode, Node target) => false;

    protected virtual Dix Insert(Dix dix, Node parentTarget) => throw new NotImplementedException();
    protected virtual Dix Remove(Dix dix, Node parentTarget, Node target) => throw new NotImplementedException();

    protected virtual Dix UpdateStructured(Dix dix, Node parentTarget) => throw new NotImplementedException();
    protected virtual Dix UpdateUnstructured(Node parentTarget, String unstructured) => throw new NotImplementedException();
}

public class MockupFileSystemSource : AbstractSource<MockupFileSystemSource.Node>
{
    DirectoryNode root = new DirectoryNode();

    public DirectoryNode Root => root;

    public MockupFileSystemSource(Action<DirectoryNode>? content = null)
    {
        content?.Invoke(root);
    }

    protected override Node? GetChild(Node parent, String name) => parent switch
    {
        DirectoryNode d => d.Children.GetValueOrDefault(name),
        _ => null
    };

    protected override Node GetRoot() => root;

    protected override Dix GetTeaser(String name, Node node) => node switch
    {
        FileNode f => D(name, f.Content),
        DirectoryNode d => D(name, from e in d.Children select D(e.Key, D(Metadata.FileSystemEntry, e.Value.EntryTypeMetadataValue))),
        _ => throw new Exception()
    };

    protected override Boolean CanUpdate(Node target) => target is FileNode;

    protected override Dix Remove(Dix dix, Node parentTarget, Node target)
    {
        if (parentTarget is DirectoryNode d && dix.Name is String name)
        {
            d.Children.Remove(name);

            return dix;
        }
        else
        {
            throw new Exception();
        }
    }

    protected override Dix Insert(Dix dix, Node parentTarget)
    {
        if (parentTarget is DirectoryNode d && dix.Name is String name)
        {
            var t = dix.GetMetadataValue(Metadata.FileSystemEntry);

            if (t is null) throw new Exception();

            Node node;

            switch (t)
            {
                case Metadata.FileSystemEntryFile:
                    if (dix.Unstructured is String u)
                    {
                        node = new FileNode { Content = u };
                    }
                    else
                    {
                        throw new Exception();
                    }
                    break;
                case Metadata.FileSystemEntryDirectory:
                    node = new DirectoryNode();
                    break;
                default:
                    throw new Exception();
            }

            d.Children.Add(name, node);

            return dix;
        }
        else
        {
            throw new Exception();
        }
    }

    public abstract class Node
    {
        public abstract String EntryTypeMetadataValue { get; }

        public virtual Node this[String name] => throw new Exception($"Not a directory");

        public virtual String Content { get => throw new Exception($"Not a file"); set { } }
    }

    public class DirectoryNode : Node
    {
        public override String EntryTypeMetadataValue => Metadata.FileSystemEntryDirectory;

        public Dictionary<String, Node> Children { get;  } = new Dictionary<String, Node>();

        public override Node this[String name] => Children.GetValueOrDefault(name);

        public DirectoryNode AddFile(String name, String content)
        {
            Children[name] = new FileNode { Content = content };
            return this;
        }

        public DirectoryNode AddDirectory(String name, Action<DirectoryNode> content)
        {
            var directory = new DirectoryNode();
            content(directory);
            Children[name] = directory;
            return this;
        }
    }

    public class FileNode : Node
    {
        public override String EntryTypeMetadataValue => Metadata.FileSystemEntryFile;

        public override String Content { get; set; } = "";
    }
}
