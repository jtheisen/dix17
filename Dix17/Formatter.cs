namespace Dix17;

public abstract class DixVisitor
{
    public virtual void Visit(Dix dix)
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

    public Formatter(Int32 level = 1)
    {
        this.level = level;
    }

    Char GetOperationCharacter(DixOperation o) => o switch
    {
        DixOperation.None => ' ',
        DixOperation.Update => '=',
        DixOperation.Insert => '+',
        DixOperation.Remove => '-',
        _ => '?'
    };

    public override void Visit(Dix dix)
    {
        writer.Write(new String(' ', level * 2 - 2));
        writer.Write(GetOperationCharacter(dix.Operation));
        writer.Write(' ');

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

    public static String Format(Dix dix)
    {
        var formatter = new Formatter();

        formatter.Visit(dix);

        return formatter.writer.ToString();
    }
}
