namespace dotnet_lox;

internal class ReturnException(object? value) : Exception
{
    public object? Value { get; } = value;
}