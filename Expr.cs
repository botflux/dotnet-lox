namespace dotnet_lox;

internal abstract record Expr
{
    public abstract R Accept<R>(IExprVisitor<R> visitor);
}