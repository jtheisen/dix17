using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dix17;

/* For this, we already need some metadata to recognize some types;
 * 
 * - tell apart arrays, objects and the rest
 * - the rest being: numbers, strings and null
 */

public class JsonStructureAwareness : IStructureAwareness
{
    public String Destructurize(Dix structure)
    {
        var jToken = MakeToken(structure);

        return JsonConvert.SerializeObject(jToken, Formatting.Indented);
    }

    public Dix Structurize(String unstructured)
    {
        var deserialized = JsonConvert.DeserializeObject<JToken>(unstructured);

        if (deserialized is null) throw new Exception("Deserialized a null");

        return MakeDix("structurized", deserialized);
    }

    static JToken MakeToken(Dix dix)
    {
        if (!dix.TryGetMetadataFlag<JsonTypeFlags>(out var jsonType)) throw new Exception($"No json metadata type");

        switch (jsonType)
        {
            case JsonTypeFlags.Boolean: return Boolean.Parse(dix.Unstructured!);
            case JsonTypeFlags.String: return dix.Unstructured!;
            case JsonTypeFlags.Number: return Int64.TryParse(dix.Unstructured, out var r) ? (JToken)r : (JToken)Double.Parse(dix.Unstructured!);
            case JsonTypeFlags.Null: return JValue.CreateNull();
            case JsonTypeFlags.Array:
                return new JArray(from i in dix.GetStructure() select MakeToken(i));
            case JsonTypeFlags.Object:
                return new JObject(
                    from p in dix.GetStructure()
                    select new JProperty(p.Name!, MakeToken(p))
                );
            default:
                throw new Exception();
        }
    }

    static Dix MakeDix(String? name, JToken token)
    {
        if (token is JObject o)
        {
            return D(name, from p in o.Properties() select MakeDix(p.Name, p.Value), Dmf(JsonTypeFlags.Object));
        }
        else if (token is JArray a)
        {
            return D(name, from i in a select MakeDix(null, i), Dmf(JsonTypeFlags.Array));
        }
        else
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return D(name, token.Value<Int64>().ToString(), Dmf(JsonTypeFlags.Number));
                case JTokenType.Float:
                    return D(name, token.Value<Double>().ToString(), Dmf(JsonTypeFlags.Number));
                case JTokenType.String:
                    return D(name, token.Value<String>()!, Dmf(JsonTypeFlags.String));
                case JTokenType.Boolean:
                    return D(name, token.Value<Boolean>().ToString(), Dmf(JsonTypeFlags.Boolean));
                case JTokenType.Null:
                    return D(name, "null", Dmf(JsonTypeFlags.Null));
                default:
                    throw new Exception($"Invalid token type {token.Type}");
            }
        }
    }
}
