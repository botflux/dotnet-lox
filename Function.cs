namespace dotnet_lox;

internal record Function(Token Name, List<Token> Params, List<Stmt> Body) : Stmt, IFunction
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
};