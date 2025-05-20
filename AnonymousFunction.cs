namespace dotnet_lox;

internal record AnonymousFunction(
    Token? Name,
    List<Token> Params,
    List<Stmt> Body
) : Expr, IFunction
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
};