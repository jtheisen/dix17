using System.Reflection;

namespace Dix17;

public interface ISource
{
    Dix Query(Dix dix);
}

public class ReflectionSource : ISource
{
    private readonly Object target;
    private readonly Assembly[] assemblies;

    public ReflectionSource(Object target, Assembly[] assemblies)
    {
        this.target = target;
        this.assemblies = assemblies;
    }

    public Dix Query(Dix dix)
    {
        return Process(dix, null, target);
    }

    Dix Process(Dix dix, Object? parentTarget, Object target) => dix.Operation switch
    {
        DixOperation.None => dix.IsLeaf()
            ? GetDixTeaser(dix.Name, target)
            : D(dix.Name,
                from d in dix.GetStructure()
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

    Dix GetPropertyDix(String name, Object target)
    {
        var type = target.GetType();

        var property = type.GetProperty(name);

        if (property is null) throw new Exception($"No property {name} on {type.FullName}");

        return GetDixTeaser(property.Name, property.GetValue(target));
    }

    Dix GetDixTeaser(String? name, Object? target)
    {
        if (target is null)
        {
            return D(name, D(Metadata.ReflectedType, Metadata.ReflectedTypeNull));
        }
        else
        {
            var type = target.GetType();

            var unstructured = target.ToString() ?? "";

            return D(name,
                unstructured,
                from p in type.GetProperties() select D(p.Name),
                D(Metadata.ReflectedClrType, type.FullName!).Singleton()
            );
        }
    }

    Object CreateObject(Dix dix, Type type)
    {
        var clrType = dix.GetMetadataValue(Metadata.ReflectedClrType);

        if (clrType is not null)
        {
            type = GetType(clrType);
        }

        if (type == typeof(String))
        {
            return dix.Unstructured ?? "";
        }
        else if (type.IsValueType)
        {
            if (dix.Unstructured is null) throw new Exception();

            return GetValueType(type, dix.Unstructured);
        }

        var instance = Activator.CreateInstance(type);

        if (instance is null) throw new Exception();

        return instance;
    }

    Object GetValueType(Type type, String text)
    {
        return Convert.ChangeType(text, type);
    }

    Type GetType(String name)
    {
        return assemblies.Select(a => a.GetType(name)).FirstOrDefault(t => t is not null) ?? throw new Exception($"Can't resolve type {name}");
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
