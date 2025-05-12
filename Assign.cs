
record Assign(Token Name, Expr Value) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor) => visitor.Visit(this);
}