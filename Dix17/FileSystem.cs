using static Dix17.MetadataForSources;

namespace Dix17;

public interface INode
{
    DixMetadata Metadata { get; }
}

public abstract class AbstractSource<Node> : ISource
    where Node : class, INode
{
    public Dix Query(Dix dix)
    {
        if (dix.IsLeaf() && dix.Operation == DixOperation.Select)
        {
            return GetTeaser(dix.Name!, GetRoot()).AddMetadata(GetTopLevelMetadata());
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
        _ => throw new Exception($"Unsupported operation for root node: {dix.Operation}")
    };

    Dix Process(Dix dix, Node? parentTarget, Node? target) => dix.Operation switch
    {
        DixOperation.Select => Select(dix, target),
        DixOperation.Update => Update(dix, parentTarget, target ?? throw new Exception($"No node to update for {dix.Name}")),
        DixOperation.Insert => InsertInternal(dix, parentTarget, target),
        DixOperation.Remove => RemoveInternal(dix, parentTarget, target),
        _ => throw new Exception($"Unsupported operation {dix.Operation}")
    };

    Dix Select(Dix dix, Node? target)
    {
        if (target is null)
        {
            return D(dix.Name).WithError("no such entry");
        }
        else if (dix.IsLeaf())
        {
            return GetTeaser(dix.Name ?? throw new Exception($"No name"), target ?? throw new Exception($"No node for {dix.Name}"));
        }
        else
        {
            return D(dix.Name, GetContent(dix, target ?? throw new Exception($"No node for {dix.Name}")));
        }
    }

    DixContent GetContent(Dix dix, Node target)
    {
        var content =
            from d in dix.Structure
            let c = GetChild(target ?? throw new Exception($"No node for {dix.Name}"), d.Name ?? throw new Exception($"No name"))
            select Process(d, target, c);

        return Dc(content.ToArray());
    }

    protected virtual DixContent GetTopLevelMetadata() => default;

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
        if (parentTarget is null) throw new Exception($"Can't remove the root");

        if (target is null) throw new Exception($"No node to remove under {dix.Name} in {parentTarget}");

        return Remove(dix, parentTarget, target);
    }

    Dix InsertInternal(Dix dix, Node? parentTarget, Node? target)
    {
        if (target is not null) throw new Exception($"Already have a node under {dix.Name}: {target}");

        if (parentTarget is null) throw new Exception($"Can't insert the root");

        return Insert(dix, parentTarget);
    }

    protected Dix WithMetadata(Dix dix, Node node)
        => dix.AddMetadata(node.Metadata);

    protected virtual Dix Insert(Dix dix, Node parentTarget) => throw new NotImplementedException();
    protected virtual Dix Remove(Dix dix, Node parentTarget, Node target) => throw new NotImplementedException();

    protected virtual Dix UpdateStructured(Dix dix, Node parentTarget) => throw new NotImplementedException();
    protected virtual Dix UpdateUnstructured(Node parentTarget, String unstructured) => throw new NotImplementedException();
}

public class MockupFileSystemSource : AbstractSource<MockupFileSystemSource.Node>
{
    DirectoryNode root = new RootNode() { Name = "/" };

    //static MetadataRuleSet schema = new MetadataRulesetBuilder()
    //    .AddRule(Dc("fs:file"), Dc("x:unstructured"))
    //    .AddRule(Dc("fs:directory"), Dc("x:structured"))
    //    .Build();

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
        FileNode f => WithMetadata(D(name, f.Content), node),
        DirectoryNode d => D(name, from e in d.Children select WithMetadata(D(e.Key), node)),
        _ => throw new Exception()
    };

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
            var t = dix.GetMetadataValue(MetadataConstants.FileSystemEntry);

            if (t is null) throw new Exception();

            Node node;

            switch (t)
            {
                case MetadataConstants.FileSystemEntryFile:
                    if (dix.Unstructured is String u)
                    {
                        node = new FileNode { Parent = parentTarget, Name = name, Content = u };
                    }
                    else
                    {
                        throw new Exception();
                    }
                    break;
                case MetadataConstants.FileSystemEntryDirectory:
                    node = new DirectoryNode() { Parent = parentTarget, Name = name };
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

    public abstract class Node : INode
    {
        public Node? Parent { get; init; }

        public String? Name { get; init; }

        public virtual String Path => $"{Parent?.Path}/{Name ?? throw new Exception("no name")}";

        public override String ToString() => Path;

        public abstract String EntryTypeMetadataValue { get; }

        public virtual Node? this[String name] => throw new Exception($"Not a directory");

        public virtual String Content { get => throw new Exception($"Not a file"); set { } }

        public virtual DixMetadata Metadata => Dm(D(MetadataConstants.FileSystemEntry, EntryTypeMetadataValue));
    }

    public class DirectoryNode : Node
    {
        public override String EntryTypeMetadataValue => MetadataConstants.FileSystemEntryDirectory;

        public override DixMetadata Metadata => Dm(base.Metadata, MdnCanInsert);

        public Dictionary<String, Node> Children { get;  } = new Dictionary<String, Node>();

        public override Node? this[String name] => Children.GetValueOrDefault(name);

        public DirectoryNode AddFile(String name, String content)
        {
            Children[name] = new FileNode { Parent = this, Name = name, Content = content };
            return this;
        }

        public DirectoryNode AddDirectory(String name, Action<DirectoryNode> content)
        {
            var directory = new DirectoryNode() { Parent = this, Name = name };
            content(directory);
            Children[name] = directory;
            return this;
        }
    }

    public class RootNode : DirectoryNode
    {
        public override String Path => "/";
    }

    public class FileNode : Node
    {
        public override String EntryTypeMetadataValue => MetadataConstants.FileSystemEntryFile;

        public override String Content { get; set; } = "";

        public override DixMetadata Metadata => Dm(base.Metadata, MdnCanUpdate, MdnCanRemove);
    }
}
