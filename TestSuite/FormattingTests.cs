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
".TrimStart(),
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2")
                )
            ).Format()
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
".TrimStart(),
            D("root",
                D("some-string", "foo"),
                D("some-list",
                    D("item1"),
                    D("item2"),
                    D("x:list")
                )
            ).Format()
        );
    }
}
