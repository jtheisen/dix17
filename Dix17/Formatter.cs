namespace Dix17;

public abstract class AbstractFormatter<D> : DixVisitor
    where D : AbstractFormatter<D>, new()
{
    protected StringWriter writer = new StringWriter();
    protected Int32 level = 1;

    static Char[] problematics = "\"\\".ToArray();

    [Flags]
    protected enum LiteralWritingFlags
    {
        SurroundInDoubleQuotesAlways = 1,
        SurroundInDoubleQuotesWhenWithSurroundingWhitespace = 2,

        AllowAtStrings = 4
    }

    Boolean HasSurroundingWhiteSpace(String s)
    {
        if (s.Length == 0) return false;
        if (Char.IsWhiteSpace(s[0])) return true;
        if (Char.IsWhiteSpace(s[^1])) return true;
        return false;
    }

    protected void WriteSimpleLiteral(String text) => WriteLiteral(text, LiteralWritingFlags.SurroundInDoubleQuotesWhenWithSurroundingWhitespace);

    protected void WriteLiteral(String? text, LiteralWritingFlags flags)
    {
        if (text is null) return;

        var surroundInQuotes = flags.HasFlag(LiteralWritingFlags.SurroundInDoubleQuotesAlways) ||
            (flags.HasFlag(LiteralWritingFlags.SurroundInDoubleQuotesWhenWithSurroundingWhitespace) && HasSurroundingWhiteSpace(text));

        var useAtStrings = flags.HasFlag(LiteralWritingFlags.AllowAtStrings) && surroundInQuotes && text.IndexOfAny(problematics) >= 0;

        if (useAtStrings)
        {
            writer.Write('@');
        }

        if (surroundInQuotes) writer.Write('"');

        foreach (var c in text)
        {
            if (Char.IsControl(c))
            {
                writer.Write($@"\u{(int)c:x4}");
            }
            else if (c == '"')
            {
                writer.Write("\"\"");
            }
            else
            {
                writer.Write(c);
            }
        }

        if (surroundInQuotes) writer.Write('"');
    }

    public static String Format(Dix dix)
    {
        var formatter = new D();

        formatter.Visit(dix);

        return formatter.writer.ToString();
    }

    public override Dix Visit(Dix dix)
    {
        ++level;
        base.Visit(dix);
        --level;
        return dix;
    }
}

public class SimpleFormatter : AbstractFormatter<SimpleFormatter>
{
    public static Char GetOperationCharacter(DixOperation o) => o switch
    {
        DixOperation.Select => ' ',
        DixOperation.Update => '=',
        DixOperation.Insert => '+',
        DixOperation.Remove => '-',
        DixOperation.Error => '!',
        _ => '?'
    };

    public override Dix Visit(Dix dix)
    {
        writer.Write(new String(' ', level * 2 - 2));
        writer.Write(GetOperationCharacter(dix.Operation));
        writer.Write(' ');

        if (dix.Name is not null)
        {
            WriteSimpleLiteral(dix.Name);
        }
        else
        {
            writer.Write("-");
        }

        if (dix.HasEmptyContent)
        {
            writer.WriteLine(" empty");
        }
        else if (dix.Unstructured is not null)
        {
            writer.Write(" = ");

            WriteSimpleLiteral(dix.Unstructured);

            writer.WriteLine();
        }
        else
        {
            writer.WriteLine();
        }

        return base.Visit(dix);
    }
}

public class CSharpFormatter : AbstractFormatter<CSharpFormatter>
{
    Char GetOperatorCharacter(DixOperation o) => o switch
    {
        DixOperation.Select => ' ',
        DixOperation.Update => '~',
        DixOperation.Insert => '+',
        DixOperation.Remove => '-',
        DixOperation.Error => '!',
        _ => throw new Exception()
    };

    Boolean pendingNewline = false;

    public CSharpFormatter()
    {
        level = 3;
    }

    void WriteCSharpLiteral(String? text)
    {
        if (text is null)
        {
            writer.Write("null");
        }
        else
        {
            WriteLiteral(text, LiteralWritingFlags.SurroundInDoubleQuotesAlways | LiteralWritingFlags.AllowAtStrings);
        }
    }

    public override Dix Visit(Dix dix)
    {
        if (pendingNewline)
        {
            writer.WriteLine(",");
            pendingNewline = false;
        }

        writer.Write(new String(' ', level * 4 - 1));
        writer.Write(GetOperatorCharacter(dix.Operation));

        writer.Write($"D(");

        WriteCSharpLiteral(dix.Name);

        if (dix.Unstructured is not null)
        {
            writer.Write($", ");

            WriteCSharpLiteral(dix.Unstructured);
        }

        pendingNewline = true;

        var result = base.Visit(dix);

        writer.Write(")");

        return result;
    }
}
