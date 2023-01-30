namespace TestSuite;

[TestClass]
public class FileSystemTests
{
    ISource source;

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
            source.Query(D("query"))
        );
    }
}
