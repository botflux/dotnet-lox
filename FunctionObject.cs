namespace dotnet_lox;

internal class FunctionObject(Function declaration, LoxEnvironment closure) : ICallable
{
    public int Arity { get; } = declaration.Params.Count;
    
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var environment = new LoxEnvironment(closure);

        for (var i = 0; i < arguments.Count; i++)
        {
            environment.Define(declaration.Params.ElementAt(i), arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnException ex)
        {
            return ex.Value;
        }

        return null;
    }

    public override string? ToString()
    {
        return $"<fn {declaration.Name.Lexeme}>";
    }
}