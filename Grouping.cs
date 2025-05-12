record Grouping(
    Expr Expression
) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}