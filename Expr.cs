abstract record Expr
{
    public abstract R Accept<R>(IExprVisitor<R> visitor);
}