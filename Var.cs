namespace dotnet_lox;

internal record Var(Token Name, Expr? Initializer) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
}