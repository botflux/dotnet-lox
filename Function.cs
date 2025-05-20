namespace dotnet_lox;

internal record Function(Token? Name, List<Token> Params, List<Stmt> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
};