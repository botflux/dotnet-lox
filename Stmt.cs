namespace dotnet_lox;

internal abstract record Stmt
{
    public abstract T Accept<T>(IStmtVisitor<T> visitor);
}