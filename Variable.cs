record Variable(Token Name) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor) => visitor.Visit(this);
}