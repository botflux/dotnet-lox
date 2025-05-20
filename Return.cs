namespace dotnet_lox;

internal record Return(Token Keyword, Expr? Value) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
};