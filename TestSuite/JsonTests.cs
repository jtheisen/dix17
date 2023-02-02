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
    json:type:object
    some-bool = True
      json:type:boolean
    some-null = null
      json:type:null
    some-array
      json:type:array
      - = 42
        json:type:number
      - = null
        json:type:null
      - = True
        json:type:boolean
      - = text
        json:type:string
      -
        json:type:object
      -
        json:type:array
    empty-object
      json:type:object
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
