namespace dotnet_lox;

class LoxEnvironment
{
    private readonly LoxEnvironment? _enclosing;
    private readonly Dictionary<string, object?> _values = new();

    public LoxEnvironment()
    {
        _enclosing = null;
    }

    public LoxEnvironment(LoxEnvironment enclosing)
    {
        _enclosing = enclosing;
    }
    
    public void Define(Token name, object? value) => _values.Add(name.Lexeme, value);

    public object? Get(Token name)
    {
        return _values.TryGetValue(name.Lexeme, out var value)
            ? value
            : _enclosing != null
                ? _enclosing.Get(name)
                : throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (_enclosing == null) throw new RuntimeError(name, $"Undefined variable '{name}'.");

        _enclosing.Assign(name, value);
    }
}