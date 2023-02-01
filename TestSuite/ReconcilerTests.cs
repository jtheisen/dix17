using Dix17.Sources;

namespace TestSuite;

[TestClass]
public class ReconcilerTests
{
    Reconciler reconciler = new Reconciler();
    MockupFileSystemSource source;

    public ReconcilerTests()
    {
        source = new MockupFileSystemSource(c => c
            .AddFile(".gitignore", "bin\nobj")
            .AddDirectory("docs", c2 => c2
                .AddFile("tutorial.md", "todo"))
            .AddDirectory("src", c2 => c2
                .AddFile("core.cs", "throw new NotImplementedException()")
                .AddFile("extensions.cs", "// todo"))
        );
    }

    [TestMethod]
    public void TestCopying()
    {
        reconciler.Copy(source.Reroot("src"), source.Reroot("docs"));

        DixValidator.AssertEqual(
            D("query",
                D("docs",
                    D("tutorial.md"),
                    D("core.cs"),
                    D("extensions.cs"))),
            source.Query(Dq(Dc("docs"))).RecursivelyRemoveMetadata()
        );
    }
}
