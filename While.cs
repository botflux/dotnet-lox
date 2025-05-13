namespace dotnet_lox;

internal record While(Expr Condition, Stmt Body) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
};