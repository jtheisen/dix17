namespace Dix17;

public class Reconciler
{
    public void Copy(ISource source, ISource target)
    {
        var sourceDix = source.Query(D("query"));

        foreach (var item in sourceDix.GetStructure())
        {
            target.Query(+item);
        }
    }
}
