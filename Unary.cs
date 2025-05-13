namespace dotnet_lox;

internal record Unary(
    Token Op,
    Expr Right
) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}