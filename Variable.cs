namespace dotnet_lox;

internal record Variable(Token Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}