namespace dotnet_lox;

internal interface IFunction
{
    Token? Name { get; }
    List<Token> Params { get; }
    List<Stmt> Body { get; }
}