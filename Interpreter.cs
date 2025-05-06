class Interpreter : IVisitor<object?>
{
    public void Interpret(Expr expression)
    {
        try
        {
            var value = Evaluate(expression);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeError error)
        {
            Reporter.RuntimeError(error);
        }
    }

    public object? Visit(Binary binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        return (binary.Op.Type, left, right) switch
        {
            (TokenType.Minus, double l, double r) => l - r,
            (TokenType.Minus, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.Slash, double l, double r) => l / r,
            (TokenType.Slash, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.Star, double l, double r) => l * r,
            (TokenType.Star, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.Plus, string l, string r) => l + r,
            (TokenType.Plus, double l, double r) => l + r,
            (TokenType.Plus, _, _) => throw new RuntimeError(binary.Op, "Operands must be two numbers or two strings."),
            (TokenType.Greater, double l, double r) => l > r,
            (TokenType.Greater, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.GreaterEqual, double l, double r) => l >= r,
            (TokenType.GreaterEqual, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.Less, double l, double r) => l < r,
            (TokenType.Less, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.LessEqual, double l, double r) => l <= r,
            (TokenType.LessEqual, _, _) => throw new RuntimeError(binary.Op, "Operand must be a numbers"),
            (TokenType.BangEqual, _, _) => !IsEqual(left, right),
            (TokenType.EqualEqual, _, _) => IsEqual(left, right),

            _ => null,
        };
    }

    public object? Visit(Grouping grouping)
    {
        return Evaluate(grouping.Expression);
    }

    public object? Visit(Unary unary)
    {
        object? right = Evaluate(unary.Right);

        return (unary.Op.Type, right) switch
        {
            (TokenType.Bang, _) => !IsTruthy(right),
            (TokenType.Minus, double r) => -r,
            (TokenType.Minus, _) => throw new RuntimeError(unary.Op, "Operand must be a number"),
            _ => null,
        };
    }

    public object? Visit(Literal literal)
    {
        return literal.Value;
    }

    object? Evaluate(Expr expression)
    {
        return expression.Accept(this);
    }

    bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            _ => true,
        };
    }

    bool IsEqual(object? a, object? b)
    {
        return (a, b) switch
        {
            (null, null) => true,
            (null, _) => false,
            _ => a.Equals(b),
        };
    }

    string? Stringify(object? obj)
    {
        return obj switch
        {
            null => "nil",
            double d => StringifyDouble(d),
            _ => obj.ToString(),
        };
    }

    string StringifyDouble(double d)
    {
        var text = d.ToString();

        if (text.EndsWith(".0"))
        {
            return text.Substring(0, text.Length - 2);
        }

        return text;
    }
}