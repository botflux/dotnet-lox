namespace dotnet_lox;

internal record Print(Expr Expression) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
}