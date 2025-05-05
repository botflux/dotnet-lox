record Literal(
    object? Value
) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}