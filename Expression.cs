abstract record Expr
{
    public abstract R Accept<R>(IVisitor<R> visitor);
}