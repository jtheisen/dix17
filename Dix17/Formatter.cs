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

public abstract class AbstractFormatter<D> : DixVisitor
    where D : AbstractFormatter<D>, new()
{
    protected StringWriter writer = new StringWriter();
    protected Int32 level = 1;

    public static String Format(Dix dix)
    {
        var formatter = new D();

        formatter.Visit(dix);

        return formatter.writer.ToString();
    }

    public override void Visit(Dix dix)
    {
        ++level;
        base.Visit(dix);
        --level;
    }
}

public class SimpleFormatter : AbstractFormatter<SimpleFormatter>
{
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

        base.Visit(dix);
    }
}

public class CSharpFormatter : AbstractFormatter<CSharpFormatter>
{
    Char GetOperatorCharacter(DixOperation o) => o switch
    {
        DixOperation.None => ' ',
        DixOperation.Update => '!',
        DixOperation.Insert => '+',
        DixOperation.Remove => '-',
        _ => throw new Exception()
    };

    Char[] problematics = "\"\\".ToArray();

    String CreateStringLiteral(String? name)
        => name is null ? "null" : name.IndexOfAny(problematics) >= 0 ? $"@\"{name.Replace("\"", "\"\"")}\"" : $"\"{name}\"";

    Boolean pendingNewline = false;

    public override void Visit(Dix dix)
    {
        if (pendingNewline)
        {
            writer.WriteLine(",");
            pendingNewline = false;
        }

        writer.Write(new String(' ', level * 4 - 1));
        writer.Write(GetOperatorCharacter(dix.Operation));

        writer.Write($"D({CreateStringLiteral(dix.Name)}");

        if (dix.Unstructured is not null)
        {
            writer.Write($", {CreateStringLiteral(dix.Unstructured)}");
        }

        pendingNewline = true;

        base.Visit(dix);

        writer.Write(")");
    }
}
