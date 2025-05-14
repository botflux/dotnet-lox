namespace dotnet_lox;

internal record Break() : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
};