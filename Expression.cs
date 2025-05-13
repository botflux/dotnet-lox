namespace dotnet_lox;

internal record Expression(Expr Expr) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
}