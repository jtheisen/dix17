using Dix17.Sources;

namespace TestSuite;

public static class TestExtensions
{
    public static String Frame(this String text)
        => $"\n{text.Trim('\n', '\r')}\n";
}

[TestClass]
public class ReflectorTests
{
    ReflectionSource source;

    public ReflectorTests()
    {
        source = new ReflectionSource(testObject, new TypeAwareness(typeof(String).Assembly));
    }

    public class NestedTestType
    {
        public Int32 Number { get; set; }

        public DateTimeOffset DateTimeOffset { get; set; }
    }

    public class TestType
    {
        public String String { get; set; } = "Hello!";

        public Boolean Boolean { get; set; } = true;

        public NestedTestType NestedObject => new NestedTestType { Number = 42 };

        public Int32[] Array { get; set; } = new[] { 1, 2 };
    }

    TestType testObject = new TestType();

    [TestMethod]
    public void TestSourceSimple()
    {
        Assert.AreEqual(@"
  query
    reflection:clr-type = TestSuite.ReflectorTests+TestType
    String
      reflection:clr-type = System.String
    Boolean
      reflection:clr-type = System.Boolean
    NestedObject
      reflection:clr-type = TestSuite.ReflectorTests+NestedTestType
    Array
      reflection:clr-type = System.Int32[]
".Frame(),
            source.Query(D("query")).Format().Frame()
        );
    }

    [TestMethod]
    public void TestSourceNestedObject()
    {
        DixValidator.AssertEqual(
            D("query",
                D("NestedObject",
                    Dm("reflection:clr-type", "TestSuite.ReflectorTests+NestedTestType"),
                    D("Number",
                        Dm("reflection:clr-type", "System.Int32")),
                    D("DateTimeOffset",
                        Dm("reflection:clr-type", "System.DateTimeOffset")))),
            source.Query(D("query", D("NestedObject"))),
            DixValidatorFlags.IgnoreExtraUnstructuredIfNullInExpected
        );
    }

    [TestMethod]
    public void TestRecursion()
    {
        AmbientBreakOnError.Enable();

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

    [TestMethod]
    public void TestStringModification()
    {
        source.Query(D("query", ~D("String", "Modified!")));

        Assert.AreEqual("Modified!", testObject.String);
    }

    [TestMethod]
    public void TestBooleanModification()
    {
        source.Query(D("query", ~D("Boolean", "false"))).AssertSuccess();

        Assert.AreEqual(false, testObject.Boolean);
    }
}
