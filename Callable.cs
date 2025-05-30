namespace dotnet_lox;

internal interface ICallable
{
    int Arity { get; }
    object? Call(Interpreter interpreter, List<object?> arguments);
}