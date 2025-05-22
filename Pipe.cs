namespace dotnet_lox;

internal record Pipe(Expr Left, Token OperatorToken, Expr Right) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor)
    {
        return visitor.VisitPipeExpr(this);
    }
}
