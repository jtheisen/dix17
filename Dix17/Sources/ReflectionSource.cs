using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Dix17.Sources;

public interface ISource
{
    Dix Query(Dix dix);
}

public class NicerTypeConverter
{
    TypeConverter converter;
    Boolean canConvert;

    public NicerTypeConverter(Type type)
    {
        converter = TypeDescriptor.GetConverter(type);
        canConvert = converter.CanConvertFrom(typeof(String)) && converter.CanConvertTo(typeof(String));
    }

    public Boolean CanConvert => canConvert;

    public String ToString(Object value)
        => converter.ConvertToInvariantString(value) ?? throw new Exception("TypeConverter failed us");

    public Object FromString(String text)
        => converter.ConvertFromInvariantString(text) ?? throw new Exception($"TypeConverter failed us");

    static ConcurrentDictionary<Type, NicerTypeConverter> instances = new ConcurrentDictionary<Type, NicerTypeConverter>();

    public static NicerTypeConverter GetConverter(Type type) => instances.GetOrAdd(type, t => new NicerTypeConverter(t));
}

public class TypeAwareness
{
    private readonly Assembly[] assemblies;

    public TypeAwareness(params Assembly[] extraAssemblies)
    {
        assemblies = new[] { typeof(String).Assembly }.Concat(extraAssemblies).ToArray();
    }

    public String CreateText(Object instance)
    {
        var type = instance.GetType();

        var converter = NicerTypeConverter.GetConverter(type);

        return converter.ToString(instance);
    }

    public Object CreateObject(Type? type, String? text, String? typeName)
    {
        if (typeName is not null)
        {
            type = GetType(typeName);
        }

        if (type is null) throw new Exception($"Dont have a type");

        if (text is not null)
        {
            var converter = NicerTypeConverter.GetConverter(type);

            return converter.FromString(text);
        }
        else
        {
            return Activator.CreateInstance(type)!;
        }
    }

    Type GetType(String name)
    {
        return assemblies.Select(a => a.GetType(name)).FirstOrDefault(t => t is not null) ?? throw new Exception($"Can't resolve type {name}");
    }
}

public class ReflectionSource : ISource
{
    private readonly Object target;
    private readonly TypeAwareness typeAwareness;

    public ReflectionSource(Object target, TypeAwareness typeAwareness)
    {
        this.target = target;
        this.typeAwareness = typeAwareness;
    }

    public Dix Query(Dix dix)
    {
        return Process(dix, null, target);
    }

    Dix Process(Dix dix, Object? parentTarget, Object target) => dix.Operation switch
    {
        DixOperation.Select => dix.IsLeaf()
            ? GetDixTeaser(dix.Name, target)
            : D(dix.Name,
                from d in dix.Structure
                select Process(d, target, d.Name)
            ),
        DixOperation.Update => Update(dix, parentTarget ?? throw new Exception("root can't be updated")),
        DixOperation.Insert => Insert(dix, target),
        DixOperation.Remove => Remove(dix, target),
        _ => throw new Exception($"Unsupported operation {dix.Operation}")
    };

    Dix Process(Dix dix, Object parentTarget, String? propertyName)
    {
        var type = target.GetType();

        var property = type.GetProperty(propertyName);

        var value = property.GetValue(parentTarget);

        return Process(dix, parentTarget, value);
    }

    Dix GetDixTeaser(String? name, Object? target)
    {
        if (target is null)
        {
            return D(name, D(MetadataConstants.ReflectedType, MetadataConstants.ReflectedTypeNull));
        }
        else
        {
            var type = target.GetType();

            var unstructured = target.ToString();

            var structure = (from p in type.GetProperties() select D(p.Name)).ToArray();

            return D(name,
                structure.Length == 0 ? unstructured : null,
                from p in type.GetProperties() select D(p.Name),
                D(MetadataConstants.ReflectedClrType, type.FullName!).Singleton()
            );
        }
    }

    Object CreateObject(Dix dix, Type type)
    {
        var clrType = dix.GetMetadataValue(MetadataConstants.ReflectedClrType);

        return typeAwareness.CreateObject(type, dix.Unstructured, clrType);
    }

    Dix Update(Dix dix, Object target)
    {
        var type = target.GetType();

        var name = dix.Name;

        if (name is null) throw new Exception();

        var property = type.GetProperty(name);

        if (property is null) throw new Exception($"No property {name} on {type.FullName}");

        property?.SetValue(target, CreateObject(dix, property.PropertyType));

        return dix;
    }

    Dix Insert(Dix dix, Object target) => throw new NotImplementedException();
    Dix Remove(Dix dix, Object target) => throw new NotImplementedException();
}
