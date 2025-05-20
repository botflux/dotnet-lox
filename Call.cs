namespace dotnet_lox;

internal record Call(Expr Callee, Token Paren, List<Expr> Arguments) : Expr
{
    public override R Accept<R>(IExprVisitor<R> visitor) => visitor.Visit(this);
};