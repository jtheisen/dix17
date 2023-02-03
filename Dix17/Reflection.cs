using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Dix17;

public static class Reflection
{
    public static Type? GetInterface(this Type type, Type genericTypeDefinition)
    {
        return type.GetInterface(genericTypeDefinition.Name);
    }

    public static Boolean TryGetCollectionItemType(this Type type, [NotNullWhen(true)] out Type? itemType)
    {
        itemType = null;

        if (!type.IsAssignableTo(typeof(IEnumerable))) return false;

        var genericTypeDefinition = type.GetInterface(typeof(IEnumerable<>));

        if (genericTypeDefinition is null) return false;

        itemType = genericTypeDefinition.GenericTypeArguments[0];

        return true;
    }

}
