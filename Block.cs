namespace dotnet_lox;

internal record Block(List<Stmt> Statements) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
}