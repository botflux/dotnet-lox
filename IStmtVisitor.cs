namespace dotnet_lox;

internal interface IStmtVisitor<T>
{
    T Visit(Print print);
    T Visit(Expression expression);
    T Visit(Var var);
    T Visit(Block block);
    T Visit(If @if);
    T Visit(While @while);
    T Visit(Return stmt);
}