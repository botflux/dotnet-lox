record Unary(
    Token Op,
    Expr Right
) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor)
    {
        return visitor.Visit(this);
    }
}