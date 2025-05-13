using System.Text;

namespace dotnet_lox;

class AstPrinter : IExprVisitor<string>
{
    
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }

    public string Visit(Variable variable)
    {
        throw new NotImplementedException();
    }

    public string Visit(Assign assign)
    {
        throw new NotImplementedException();
    }
    
    public string Visit(Binary binary)
    {
        return Parenthesize(binary.Op.Lexeme, binary.Left, binary.Right);
    }

    public string Visit(Grouping grouping)
    {
        return Parenthesize("group", grouping.Expression);
    }

    public string Visit(Unary unary)
    {
        return Parenthesize(unary.Op.Lexeme, unary.Right);
    }

    public string Visit(Literal literal)
    {
        return literal.Value?.ToString() ?? "nil";
    }

    public string Visit(Logical logical)
    {
        throw new NotImplementedException();
    }

    private string Parenthesize(string name, params Expr[] exprs)
    {
        var builder = new StringBuilder();

        builder.Append("(").Append(name);

        foreach (var expr in exprs)
        {
            builder.Append(" ");
            builder.Append(expr.Accept(this));
        }

        builder.Append(")");

        return builder.ToString();
    }
}