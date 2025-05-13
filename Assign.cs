namespace dotnet_lox;

internal record Assign(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}