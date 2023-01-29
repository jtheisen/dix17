using Newtonsoft.Json;

namespace TestSuite;

[TestClass]
public class JsonTests
{
    String testJson = """
{
  "some-bool": true,
  "some-null": null,
  "some-array": [ 42, null, true, "text", { }, [ ] ],
  "empty-object": { }
}
""";

    [TestMethod]
    public void TestSerialization()
    {
        var json = new JsonStructureAwareness();

        Assert.AreEqual(
            @"
  structurized
    x:json-type = object
    some-bool = True
      x:json-type = boolean
    some-null = null
      x:json-type = null
    some-array
      x:json-type = array
      - = 42
        x:json-type = number
      - = null
        x:json-type = null
      - = True
        x:json-type = boolean
      - = text
        x:json-type = string
      -
        x:json-type = object
      -
        x:json-type = array
    empty-object
      x:json-type = object
".Frame(),
            json.Structurize(testJson).Format().Frame()
        );
    }

    [TestMethod]
    public void TestRedeserialization()
    {
        var json = new JsonStructureAwareness();

        var dix = json.Structurize(testJson);

        var redestructurizedJson = json.Destructurize(dix);

        var deserializedJson = JsonConvert.DeserializeObject(testJson);
        var reserializedJson = JsonConvert.SerializeObject(deserializedJson, Formatting.Indented);

        Assert.AreEqual(reserializedJson, redestructurizedJson);
    }
}
