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
        source = new ReflectionSource(testObject, new[] { typeof(String).Assembly });
    }

    public class NestedTestType
    {
        public Int32 Number { get; set; }
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
    public void TestReflection()
    {
        var reflector = new Reflector();

        Assert.AreEqual(
            @"
  root
    reflection:type = object
    String = Hello!
      reflection:type = string
    Boolean = True
      reflection:type = boolean
    NestedObject
      reflection:type = object
      Number = 42
        reflection:type = number
    Array
      reflection:type = enumerable
      - = 1
        reflection:type = number
      - = 2
        reflection:type = number
".Frame(),
            reflector.GetDix("root", testObject, 10).Format().Frame()
        );
    }

    [TestMethod]
    public void TestSourceSimple()
    {
        Assert.AreEqual(@"
  query = TestSuite.ReflectorTests+TestType
    reflection:clr-type = TestSuite.ReflectorTests+TestType
    String
    Boolean
    NestedObject
    Array
".Frame(),
            source.Query(D("query")).Format().Frame()
        );
    }

    [TestMethod]
    public void TestSourceNestedObject()
    {
        Assert.AreEqual(@"
  query
    NestedObject = TestSuite.ReflectorTests+NestedTestType
      reflection:clr-type = TestSuite.ReflectorTests+NestedTestType
      Number
".Frame(),
            source.Query(D("query", D("NestedObject"))).Format().Frame()
        );
    }

    [TestMethod]
    public void TestStringModification()
    {
        source.Query(D("query", !D("String", "Modified!")));

        Assert.AreEqual("Modified!", testObject.String);
    }

    [TestMethod]
    public void TestBooleanModification()
    {
        source.Query(D("query", !D("Boolean", "false")));

        Assert.AreEqual(false, testObject.Boolean);
    }
}
