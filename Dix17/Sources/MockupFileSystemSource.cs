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

    protected override Node GetRoot() => root;

    protected override Dix Remove(Dix dix, Node parentTarget, Node target)
    {
        if (parentTarget is DirectoryNode d && dix.Name is String name)
        {
            d.Remove(name);

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
            if (!dix.TryGetMetadataFlag<FileSystemFlags>(out var entryType)) return dix.ErrorInternal();

            Node node;

            switch (entryType)
            {
                case FileSystemFlags.File:
                    if (dix.Unstructured is String u)
                    {
                        node = new FileNode { Parent = parentTarget, Name = name, Unstructured = u };
                    }
                    else
                    {
                        node = new FileNode { Parent = parentTarget, Name = name, Unstructured = "" };
                    }
                    break;
                case FileSystemFlags.Directory:
                    node = new DirectoryNode() { Parent = parentTarget, Name = name };
                    break;
                default:
                    return dix.ErrorInternal();
            }

            d.Add(name, node);

            return dix;
        }
        else
        {
            return dix.ErrorInternal();
        }
    }

    protected override Node? GetChild(Node parent, String name)
    {
        if (parent is DirectoryNode d)
        {
            return d[name];
        }
        else
        {
            return null;
        }
    }

    public abstract class Node : INode<Node>
    {
        public Node? Parent { get; init; }

        public String? Name { get; init; }

        public virtual String Path => $"{Parent?.Path}/{Name ?? throw new Exception("no name")}";

        public override String ToString() => Path;

        public virtual Node? this[String name] => throw new Exception($"Not a directory");

        public virtual String? Unstructured { get => null; set { } }

        public virtual IEnumerable<Node>? Structured => throw new Exception($"Not a directory");

        public abstract DixMetadata Metadata { get; }
    }

    public class DirectoryNode : Node
    {
        public override DixMetadata Metadata => Dm(Dmf(FileSystemFlags.Directory), Dmf(SourceFlags.CanInsert));

        List<String> childrenInOrder = new List<String>();
        Dictionary<String, Node> childrenDict { get; } = new Dictionary<String, Node>();

        public override Node? this[String name] => childrenDict.GetValueOrDefault(name);

        public override IEnumerable<Node>? Structured => from c in childrenInOrder select childrenDict[c];

        public void Remove(String name)
        {
            childrenInOrder.Remove(name);
            childrenDict.Remove(name);
        }

        public void Add(String name, Node node)
        {
            childrenInOrder.Add(name);
            childrenDict.Add(name, node);
        }

        public DirectoryNode AddFile(String name, String content)
        {
            childrenInOrder.Add(name);
            childrenDict[name] = new FileNode { Parent = this, Name = name, Unstructured = content };
            return this;
        }

        public DirectoryNode AddDirectory(String name, Action<DirectoryNode> content)
        {
            var directory = new DirectoryNode() { Parent = this, Name = name };
            content(directory);
            childrenInOrder.Add(name);
            childrenDict[name] = directory;
            return this;
        }
    }

    public class RootNode : DirectoryNode
    {
        public override String Path => "/";
    }

    public class FileNode : Node
    {
        public override String? Unstructured { get; set; } = "";

        public override DixMetadata Metadata => Dm(Dmf(FileSystemFlags.File), Dmf(SourceFlags.CanUpdate), Dmf(SourceFlags.CanRemove));
    }
}
