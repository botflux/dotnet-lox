class ReversePolishNotationAstPrinter : IExprVisitor<string>
{
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }

    public string Visit(Binary binary)
    {
        return $"{binary.Left.Accept(this)} {binary.Right.Accept(this)} {binary.Op.Lexeme}";
    }

    public string Visit(Grouping grouping)
    {
        return grouping.Expression.Accept(this);
    }

    public string Visit(Unary unary)
    {
        return $"{unary.Right.Accept(this)} {unary.Op.Lexeme}";
    }

    public string Visit(Literal literal)
    {
        return literal.Value?.ToString() ?? "nil";
    }

    public string Visit(Variable variable)
    {
        throw new NotImplementedException();
    }

    public string Visit(Assign assign)
    {
        throw new NotImplementedException(); 
    }

    public string Visit(Logical logical)
    {
        throw new NotImplementedException();
    }
}