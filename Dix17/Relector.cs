using System.Collections;

namespace Dix17;

public class Reflector
{
    public IDix GetDix(String? name, Object? target, Int32 depth)
    {
        if (depth == 0)
        {
            return D(name);
        }
        else if (target is null)
        {
            return D(name, D(Metadata.ReflectedType, Metadata.ReflectedTypeNull));
        }
        else if (target is Boolean flag)
        {
            return D(name, flag.ToString(), D(Metadata.ReflectedType, Metadata.ReflectedTypeBoolean));
        }
        else if (target is String text)
        {
            return D(name, text, D(Metadata.ReflectedType, Metadata.ReflectedTypeString));
        }
        else if (target is IEnumerable items)
        {
            return D(name, from i in items.Cast<Object>() select GetDix(null, i, depth - 1), D(Metadata.ReflectedType, Metadata.ReflectedTypeEnumerable));
        }
        else if (numberTypes.Contains(target.GetType()))
        {
            return D(name, target.ToString()!, D(Metadata.ReflectedType, Metadata.ReflectedTypeNumber));
        }
        else
        {
            var type = target.GetType();

            return D(name, from p in type.GetProperties() select GetDix(p.Name, p.GetValue(target), depth - 1), D(Metadata.ReflectedType, Metadata.ReflectedTypeObject));
        }
    }

    static readonly Type[] numberTypes = new[] { typeof(Int32), typeof(Double) }; 
}

public interface ISource
{
    IDix Query(IDix dix);
}

public class RelfectionSource : ISource
{
    private readonly Reflector reflector;
    private readonly Object target;

    public RelfectionSource(Object target, Reflector reflector)
    {
        this.target = target;
        this.reflector = reflector;
    }

    public IDix Query(IDix dix)
    {
        return Process(dix, target);
    }

    IDix Process(IDix dix, Object target)
    {
        return dix.IsLeaf()
            ? reflector.GetDix(dix.Name, target, 1)
            : D(dix.Name,
                from d in dix.GetStructure()
                let nested = target.GetType().GetProperty(d.Name!)?.GetValue(target)
                select Process(d, nested)
            );
    }
}
