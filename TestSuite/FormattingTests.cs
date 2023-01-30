namespace TestSuite;

[TestClass]
public class FormattingTests
{
    [TestMethod]
    public void TestBasicFormatting()
    {
        Assert.AreEqual(
            @"
  root
    some-string = foo
    some-list
      item1
      item2
".Frame(),
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2")
                )
            ).Format().Frame()
        );
    }

    [TestMethod]
    public void TestOperationFormatting()
    {
        Assert.AreEqual(
            @"
  root
  = some-string = foo
    some-list
    + item1
      item2
".Frame(),
            D("root",
                !D("some-string", "foo"),
                D("some-list",
                    +D("item1"),
                    D("item2")
                )
            ).Format().Frame()
        );
    }

    [TestMethod]
    public void TestMetadataFormatting()
    {
        Assert.AreEqual(
            @"
  root
    some-string = foo
    some-list
      x:list
      item1
      item2
".Frame(),
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2"),
                    D("x:list")
                )
            ).Format().Frame()
        );
    }

    [TestMethod]
    public void TestCSharpFormatting()
    {
        Assert.AreEqual(
            """
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2")))
""".Frame(),
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2")
                )
            ).FormatCSharp().Frame()
        );
    }

    [TestMethod]
    public void TestCSharpEscapingFormatting()
    {
        Assert.AreEqual(
            """
            D("root",
                D("some-string", @"fo\o"),
                D(@"some""list",
                    D("item1"),
                    D("item2")))
""".Frame(),
            D("root",
                D("some-string", @"fo\o"),
                D(@"some""list",
                    D("item1"),
                    D("item2")
                )
            ).FormatCSharp().Frame()
        );
    }
}
