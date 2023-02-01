using static Dix17.MetadataForSources;

namespace Dix17.Sources;

public class MockupFileSystemSource : NodeSource<MockupFileSystemSource.Node>
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

    protected override Dix GetTeaser(Dix dix, String name, Node node) => node switch
    {
        FileNode f => WithMetadata(D(name, f.Content), node),
        DirectoryNode d => D(name, from e in d.Children select WithMetadata(D(e.Key), node)),
        _ => dix.ErrorUnsupportedOperation()
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
            return dix.ErrorInternal();
        }
    }

    protected override Dix Insert(Dix dix, Node parentTarget)
    {
        if (parentTarget is DirectoryNode d && dix.Name is String name)
        {
            var t = dix.GetMetadataValue(MetadataConstants.FileSystemEntry);

            if (t is null) return dix.ErrorInternal();

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
                        return dix.ErrorInternal();
                    }
                    break;
                case MetadataConstants.FileSystemEntryDirectory:
                    node = new DirectoryNode() { Parent = parentTarget, Name = name };
                    break;
                default:
                    return dix.ErrorInternal();
            }

            d.Children.Add(name, node);

            return dix;
        }
        else
        {
            return dix.ErrorInternal();
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

        public Dictionary<String, Node> Children { get; } = new Dictionary<String, Node>();

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
