using Dix17.Sources;
using System.Diagnostics.CodeAnalysis;

namespace Dix17;

public static partial class Extensions
{
    public static void Foreach(this IEnumerable<Dix>? items, Action<Dix> action)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            action(item);
        }
    }

    public static void ForeachName(this IEnumerable<Dix>? items, Action<String> action)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            if (item.Name is null) continue;

            action(item.Name);
        }
    }

    public static void ForeachName<T>(this IEnumerable<Dix>? items, Func<String, T> action)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            if (item.Name is null) continue;

            action(item.Name);
        }
    }

    public static IEnumerable<String?> GetNames(this IEnumerable<Dix> items)
        => items.Select(i => i.Name).Where(i => i is not null);

    public static IEnumerable<T> WhereValueNotNull<T>(this IEnumerable<T?> items)
        where T : struct
    {
        foreach (var item in items)
        {
            if (item is T t) yield return t;
        }
    }

    public static IEnumerable<(String name, Dix? l, Dix? r)> Zip(this IEnumerable<Dix> lhs, IEnumerable<Dix> rhs)
    {
        var inLhsOrBoth =
            from ld in lhs
            where ld.Name is not null
            join rd in rhs on ld.Name equals rd.Name into rds
            from rd in rds.Cast<Dix?>().DefaultIfEmpty()
            select (ld.Name, (Dix?)ld, rd);

        var inRhsButNotInLhs =
            from rd in rhs
            where rd.Name is not null
            join ld in lhs on rd.Name equals ld.Name into lds
            where !lds.Any()
            select (rd.Name, (Dix?)null, (Dix?)rd);

        return inLhsOrBoth.Concat(inRhsButNotInLhs);
    }
}

public class NameStack : IDisposable
{
    private readonly Stack<String?> path = new Stack<String?>();

    public Boolean TryGetNonNullArray([NotNullWhen(true)] out String[]? path)
    {
        var result = this.path.ToArray();

        if (result.Contains(null))
        {
            path = null;
            return false;
        }
        else
        {
            path = result!;
            return true;
        }
    }

    public Boolean IsEmpty => path.Count == 0;

    public override String ToString() => String.Join("/", path);

    public IDisposable Push(String? name)
    {
        path.Push(name);

        return this;
    }

    void IDisposable.Dispose()
    {
        path.Pop();
    }
}

public class RecursiveQueryRunner
{
    private readonly ISource source;
    private readonly NameStack path;

    Boolean hadError;

    public RecursiveQueryRunner(ISource source)
    {
        this.source = source;
        path = new NameStack();
    }

    public Dix Recurse(String? name = DefaultQueryName, Int32? maxLevel = null)
    {
        return Recurse(source.Query(D(name)), maxLevel);
    }

    Dix Recurse(Dix dix, Int32? maxLevel = null)
    {
        if (hadError)
        {
            return dix;
        }
        else if (dix.Operation == DixOperation.Error)
        {
            hadError = true;

            return dix;
        }
        else if (maxLevel == 0)
        {
            return dix;
        }
        else if (dix.HasNilContent)
        {
            if (path.IsEmpty)
            {
                return dix.Error("Source returned a nil at the root");
            }
            else if (path.TryGetNonNullArray(out var properPath))
            {
                return new RecursiveQueryRunner(source.Reroot(properPath)).Recurse(dix.Name, maxLevel - 1);
            }
            else
            {
                return dix.Error($"Can't recurse into a path with unnamed nodes");
            }
        }
        else if (dix.Structure is IEnumerable<Dix> structure)
        {
            var result = new List<Dix>();

            foreach (var d in structure)
            {
                using var _ = path.Push(d.Name);

                result.Add(Recurse(d, maxLevel - 1));
            }

            return dix.WithStructure(result);
        }
        else
        {
            return dix;
        }
    }
}

public static partial class Extensions
{
    public static Dix QueryRecursively(this ISource source, Int32? maxLevel = null)
        => new RecursiveQueryRunner(source).Recurse(maxLevel: maxLevel);
}

public class Reconciler
{
    public Dix Copy(ISource source, ISource target)
    {
        var atSource = source.Query(Dq());

        var atTarget = target.Query(atSource);

        var operation = GetInnerOperations(DefaultQueryName, atSource, atTarget);

        //Console.WriteLine(atSource.Format());
        //Console.WriteLine(atTarget.Format());
        //Console.WriteLine(operation.Format());

        var result = target.Query(operation);

        return result;
    }

    Dix GetInnerOperations(String name, Dix source, Dix target)
    {
        var operations = D(name,
            source.GetStructure()
            .Zip(target.GetStructure())
            .Select(p => GetOperation(p.l, p.r))
            .WhereValueNotNull()
        );

        return operations;
    }

    Dix? GetOperation(Dix? sourceOrNull, Dix? targetOrNull, Boolean removeExtra = false)
    {
        if (sourceOrNull is Dix source)
        {
            if (targetOrNull is Dix target && target.Operation != DixOperation.Error)
            {
                if (source.Unstructured is not null)
                {
                    return ~source;
                }
                else if (target.Unstructured is not null)
                {
                    return ~source;
                }
                else
                {
                    return GetInnerOperations(source.Name!, source, target);
                }
            }
            else
            {
                return +source;
            }
        }
        else if(removeExtra && targetOrNull is Dix target)
        {
            return -target;
        }
        else
        {
            return null;
        }
    }

    public Dix Check(Stack<String?> path, ISource source, ISource target, Dix query, Dix response)
    {
        if (query.HasNilContent)
        {
            return D(query.Name);
        }
        else if (response.HasNilContent)
        {
            // this an error?

            return D(query.Name);

            //var rerootPath = path.ToArray();

            //return Copy(source.Reroot(rerootPath!), target.Reroot(rerootPath!));
        }
        else
        {
            var queryChildren = query.GetStructure().ToArray();
            var responseChildren = response.GetStructure().ToArray();

            var children = new List<Dix>();

            for (var i = 0; i < queryChildren.Length; i++)
            {
                var queryChild = queryChildren[i];

                if (responseChildren.Length < i - 1) throw new Exception();

                var responseChild = responseChildren[i];

                if (queryChild.Name != responseChild.Name) throw new Exception();

                path.Push(queryChild.Name);

                try
                {
                    var child = Check(path, source, target, queryChild, responseChild);

                    children.Add(child);
                }
                finally
                {
                    path.Pop();
                }
            }

            return D(query.Name, children.ToArray());
        }
    }
}
