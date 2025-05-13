record Logical(Expr Left, Token Operator, Expr Right) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor) => visitor.Visit(this);
};