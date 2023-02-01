using Dix17.Sources;

namespace Dix17;

public class RerootedSource : ISource
{
    private readonly ISource nested;
    private readonly String[] path;

    public RerootedSource(ISource nested, String[] path)
    {
        this.nested = nested;
        this.path = path;

        if (path.Length == 0) throw new Exception("Path must have at least one item");
    }

    Dix GetDix(Dix content)
    {
        var result = content.WithName(path[path.Length - 1]);

        for (var i = path.Length - 2; i >= 0; --i)
        {
            result = D(path[i], result);
        }

        return result;
    }

    public Dix Query(Dix dix)
    {
        var query = D("query", GetDix(dix));

        var directResult = nested.Query(query);

        var result = directResult;

        for (var i = 0; i < path.Length; ++i)
        {
            var next = result.GetStructure().Single($"Expected a singleton structure from source in {result.Name}");
            result = next;
            var f = path[i];
            if (next.Name != f) throw new Exception($"Source yielded a name of {next.Name} where {f} was expected");
        }

        return result.WithName(dix.Name);
    }
}

public static partial class Extensions
{
    public static ISource Reroot(this ISource source, params String[] path)
        => path.Length > 0 ? new RerootedSource(source, path) : source;
}