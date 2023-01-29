namespace Dix17;

public abstract class DixVisitor
{
    public virtual void Visit(IDix dix)
    {
        foreach (var child in dix.GetMetadata())
        {
            Visit(child);
        }

        foreach (var child in dix.GetStructure())
        {
            Visit(child);
        }
    }
}

public class Formatter : DixVisitor
{
    StringWriter writer = new StringWriter();
    Int32 level;

    public Formatter(Int32 level = 0)
    {
        this.level = level;
    }

    public override void Visit(IDix dix)
    {
        writer.Write(new String(' ', level * 2));

        if (dix.Name is not null)
        {
            writer.Write($"{dix.Name}");
        }
        else
        {
            writer.Write("-");
        }

        if (dix.Unstructured is not null)
        {
            writer.Write(" = ");

            if (String.IsNullOrWhiteSpace(dix.Unstructured))
            {
                writer.WriteLine($"\"{dix.Unstructured}\"");
            }
            else
            {
                writer.WriteLine(dix.Unstructured);
            }
        }
        else
        {
            writer.WriteLine();
        }

        try
        {
            ++level;
            base.Visit(dix);
        }
        finally
        {
            --level;
        }
    }

    public static String Format(IDix dix)
    {
        var formatter = new Formatter();

        formatter.Visit(dix);

        return formatter.writer.ToString();
    }
}
