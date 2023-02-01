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
                .AddFile("core.cs", "throw new NotImplementedException()")
                .AddFile("extensions.cs", "// todo"))
        );
    }

    [TestMethod]
    public void TestRootQuery()
    {
        DixValidator.AssertEqual(
            D("query",
                D(".gitignore",
                    D("fs:entry", "file")),
                D("docs",
                    D("fs:entry", "directory")),
                D("src",
                    D("fs:entry", "directory"))),
            source.Query(D("query")).RecursivelyRemoveMetadataExcept("fs")
        );
    }

    [TestMethod]
    public void TestSpecificQuery()
    {
        DixValidator.AssertEqual(
            D("query",
                D("src",
                    D("core.cs",
                        D("fs:entry", "file")),
                    D("extensions.cs",
                        D("fs:entry", "file")))),
            source.Query(D("query", D("src"))).RecursivelyRemoveMetadataExcept("fs")
        );
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference

    [TestMethod]
    public void TestInsertion()
    {
        source.Query(D("query", D("docs", +D("readme.md", "testcontent!", D(MetadataConstants.FileSystemEntry, MetadataConstants.FileSystemEntryFile)))));

        Assert.AreEqual("testcontent!", source.Root["docs"]["readme.md"].Content);
    }

    [TestMethod]
    public void TestRemoval()
    {
        Assert.IsNotNull(source.Root["docs"]["tutorial.md"]);

        source.Query(D("query", D("docs", -D("tutorial.md"))));

        Assert.IsNull(source.Root["docs"]["tutorial.md"]);
    }
}
