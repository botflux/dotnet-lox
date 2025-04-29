record Grouping(
    Expr Expression
) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}