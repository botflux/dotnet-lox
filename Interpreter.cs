namespace dotnet_lox;

internal class Interpreter : IExprVisitor<object?>, IStmtVisitor<Nothing>
{
    private readonly LoxEnvironment _globals = new();
    private LoxEnvironment _environment;

    public Interpreter()
    {
        _environment = _globals;
        
        _globals.Define(
            new Token(TokenType.Identifier, "clock", null, -1), 
            new Clock()
        );
    }
    
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Reporter.RuntimeError(error);
        }
    }

    public Nothing Visit(Var var)
    {
        var value = var.Initializer != null
            ? Evaluate(var.Initializer)
            : null;

        _environment.Define(var.Name, value);
        return Nothing.N;
    }

    public Nothing Visit(Print print)
    {
        var value = Evaluate(print.Expression);
        Console.WriteLine(Stringify(value));
        return Nothing.N;
    }

    public Nothing Visit(If @if)
    {
        if (IsTruthy(Evaluate(@if.Condition)))
        {
            Execute(@if.ThenBranch);
        }
        else if (@if.ElseBranch != null)
        {
            Execute(@if.ElseBranch);
        }
        return Nothing.N;
    }

    public Nothing Visit(While @while)
    {
        while (IsTruthy(Evaluate(@while.Condition)))
        {
            Execute(@while.Body);
        }
        
        return Nothing.N;
    }

    public Nothing Visit(Function expression)
    {
        var function = new FunctionObject(expression, _environment);
        _environment.Define(expression.Name, function);
        return Nothing.N;
    }

    public Nothing Visit(Return stmt)
    {
        object? value = null;

        if (stmt.Value != null)
            value = Evaluate(stmt.Value);

        throw new ReturnException(value);
    }

    public object? Visit(Logical logical)
    {
        var left = Evaluate(logical.Left);

        if (logical.Operator.Type == TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(logical.Right);
    }

    public object? Visit(Call call)
    {
        var callee = Evaluate(call.Callee);
        var arguments = call.Arguments
            .Select(arg => Evaluate(arg))
            .ToList();

        if (callee is ICallable callable)
        {
            if (callable.Arity != arguments.Count)
            {
                throw new RuntimeError(call.Paren, $"Expect {callable.Arity} arguments but got {arguments.Count}.");
            }
            
            return callable.Call(this, arguments);
        }

        throw new RuntimeError(call.Paren, "Can only call functions and classes");
    }

    public Nothing Visit(Expression expression)
    {
        Evaluate(expression.Expr);
        return Nothing.N;
    }

    public object? Visit(Variable variable)
    {
        return _environment.Get(variable.Name);
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

    public object? Visit(Assign assign)
    {
        var value = Evaluate(assign.Value);
        _environment.Assign(assign.Name, value);
        return value;
    }

    public Nothing Visit(Block block)
    {
        ExecuteBlock(block.Statements, new LoxEnvironment(_environment));
        
        return Nothing.N;
    }

    public void ExecuteBlock(List<Stmt> statements, LoxEnvironment environment)
    {
        var previous = _environment;

        try
        {
            _environment = environment;

            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
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

    void Execute(Stmt statement)
    {
        statement.Accept(this);
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