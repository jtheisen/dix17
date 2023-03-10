using Dix17.Sources;
using System.Security.Cryptography;

namespace TestSuite;

[TestClass]
public class FileSystemTests
{
    MockupFileSystemSource source;

    public FileSystemTests()
    {
        source = new MockupFileSystemSource(c => c
            .AddFile(".gitignore", "bin\nobj")
            .AddDirectory("docs", c2 => c2
                .AddFile("tutorial.md", "todo"))
            .AddDirectory("src", c2 => c2
                .AddFile("core.cs", "DoReallySmartStuff()")
                .AddFile("extensions.cs", "// todo"))
        );
    }

    [TestMethod]
    public void TestRootQuery()
    {
        DixValidator.AssertEqual(
            D("query",
                Dm("fs:directory"),
                D(".gitignore",
                    Dm("fs:file")),
                D("docs",
                    Dm("fs:directory")),
                D("src",
                    Dm("fs:directory"))),
            source.Query(D("query")).RecursivelyRemoveMetadataExcept("fs")
        );
    }

    [TestMethod]
    public void TestSpecificQuery()
    {
        DixValidator.AssertEqual(
            D("query",
                D("src",
                    Dm("fs:directory"),
                    D("core.cs",
                        Dm("fs:file")),
                    D("extensions.cs",
                        Dm("fs:file")))),
            source.Query(D("query", D("src"))).RecursivelyRemoveMetadataExcept("fs")
        );
    }

    [TestMethod]
    public void TestRecursion()
    {
        DixValidator.AssertEqual(
            D("query",
                D(".gitignore", "bin\u000aobj"),
                D("docs",
                    D("tutorial.md", "todo")),
                D("src",
                    D("core.cs", "DoReallySmartStuff()"),
                    D("extensions.cs", "// todo"))),
            source.QueryRecursively().RecursivelyRemoveMetadata()
        );
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference

    [TestMethod]
    public void TestInsertion()
    {
        source.Query(D("query", D("docs", +D("readme.md", "testcontent!", Dmf(FileSystemFlags.File)))));

        Assert.AreEqual("testcontent!", source.Root["docs"]["readme.md"].Unstructured);
    }

    [TestMethod]
    public void TestRemoval()
    {
        Assert.IsNotNull(source.Root["docs"]["tutorial.md"]);

        source.Query(D("query", D("docs", -D("tutorial.md"))));

        Assert.IsNull(source.Root["docs"]["tutorial.md"]);
    }
}
