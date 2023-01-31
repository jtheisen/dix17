using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Dix17.AdHocCreation;

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
        var jsonType = dix.GetMetadata(MetadataConstants.JsonType);

        if (jsonType is null) throw new Exception();

        switch (jsonType?.Unstructured)
        {
            case MetadataConstants.JsonTypeBoolean: return Boolean.Parse(dix.Unstructured!);
            case MetadataConstants.JsonTypeString: return dix.Unstructured!;
            case MetadataConstants.JsonTypeNumber: return Int64.TryParse(dix.Unstructured, out var r) ? (JToken)r : (JToken)Double.Parse(dix.Unstructured!);
            case MetadataConstants.JsonTypeNull: return JValue.CreateNull();
            case MetadataConstants.JsonTypeArray:
                return new JArray(from i in dix.GetStructure() select MakeToken(i));
            case MetadataConstants.JsonTypeObject:
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
            return D(name, from p in o.Properties() select MakeDix(p.Name, p.Value), D(MetadataConstants.JsonType, MetadataConstants.JsonTypeObject));
        }
        else if (token is JArray a)
        {
            return D(name, from i in a select MakeDix(null, i), D(MetadataConstants.JsonType, MetadataConstants.JsonTypeArray));
        }
        else
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return D(name, token.Value<Int64>().ToString(), D(MetadataConstants.JsonType, MetadataConstants.JsonTypeNumber));
                case JTokenType.Float:
                    return D(name, token.Value<Double>().ToString(), D(MetadataConstants.JsonType, MetadataConstants.JsonTypeNumber));
                case JTokenType.String:
                    return D(name, token.Value<String>()!, D(MetadataConstants.JsonType, MetadataConstants.JsonTypeString));
                case JTokenType.Boolean:
                    return D(name, token.Value<Boolean>().ToString(), D(MetadataConstants.JsonType, MetadataConstants.JsonTypeBoolean));
                case JTokenType.Null:
                    return D(name, "null", D(MetadataConstants.JsonType, MetadataConstants.JsonTypeNull));
                default:
                    throw new Exception($"Invalid token type {token.Type}");
            }
        }
    }
}
