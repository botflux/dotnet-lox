namespace dotnet_lox;

internal record Literal(
    object? Value
) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}