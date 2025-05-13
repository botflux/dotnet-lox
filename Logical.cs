namespace dotnet_lox;

internal record Logical(Expr Left, Token Operator, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
};