using Dix17.Sources;

namespace TestSuite;

[TestClass]
public class RerootingTests
{
    MockupFileSystemSource source;

    public RerootingTests()
    {
        source = new MockupFileSystemSource(c => c
            .AddFile(".gitignore", "bin\nobj")
            .AddDirectory("src", c2 => c2
                .AddDirectory("bin", c3 => c3
                    .AddFile(".empty", ""))
                .AddFile("core.cs", "throw new NotImplementedException()")
                .AddFile("extensions.cs", "// todo"))
        );
    }

    [TestMethod]
    public void TestRerooting1()
    {
        var rerooted = source.Reroot("src");

        DixValidator.AssertEqual(
            D("query",
                D("bin"),
                D("core.cs"),
                D("extensions.cs")),
            rerooted.Query(D("query")).RecursivelyRemoveMetadata()
        );
    }

    [TestMethod]
    public void TestRerooting2()
    {
        var rerooted = source.Reroot("src", "bin");

        DixValidator.AssertEqual(
            D("query",
                D(".empty")),
            rerooted.Query(D("query")).RecursivelyRemoveMetadata()
        );
    }

    [TestMethod]
    public void TestRerooting3()
    {
        var rerooted = source.Reroot("src", "bin");

        DixValidator.AssertEqual(
            D("query",
                D(".empty", "")),
            rerooted.Query(D("query", D(".empty"))).RecursivelyRemoveMetadata()
        );
    }

}
