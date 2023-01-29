namespace TestSuite;

[TestClass]
public class ReflectorTests
{
    Object testObject = new
    {
        String = "Hello!",
        Boolean = true,
        NestedObject = new
        {
            Number = 42
        },
        Array = new[] { 1, 2 }
    };

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
".TrimStart(),
            reflector.GetDix("root", testObject, 10).Format()
        );
    }
}
