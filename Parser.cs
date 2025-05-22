namespace dotnet_lox;

internal class Parser
{
    private class ParseError : Exception {}

    private readonly List<Token> _tokens;
    private int _current;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        var statements = new List<Stmt>();

        while (!IsAtEnd())
        {
            var d = Declaration();

            if (d != null)
            {
                statements.Add(d);
            }
        }

        return statements;
    }

    Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Fun))
            {
                return FunctionDeclaration("function");
            }
            
            if (Match(TokenType.Var))
            {
                return VarDeclaration();
            }

            return Statement();
        }
        catch (ParseError)
        {
            Synchonize();
            return null;
        }
    }

    private Stmt? FunctionDeclaration(string kind)
    {
        var name = Consume(TokenType.Identifier, $"Expect {kind} name");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
        var parameters = new List<Token>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 parameters.");
                }
                
                parameters.Add(
                    Consume(TokenType.Identifier, "Expect parameter name.")
                );
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expect ')' after parameters.");
        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        var body = Block();
        
        return new Function(name, parameters, body);
    }

    Stmt VarDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expect variable name.");
        var initializer = Match(TokenType.Equal)
            ? Expression()
            : null;

        Consume(TokenType.SemiColon, "Expect ';' after variable declaration.");
        return new Var(name, initializer);
    }

    Stmt Statement()
    {
        if (Match(TokenType.For))
        {
            return ForStatement();
        }
        
        if (Match(TokenType.If))
        {
            return IfStatement();
        }

        if (Match(TokenType.While))
        {
            return WhileStatement();
        }
        
        if (Match(TokenType.Print))
        {
            return PrintStatement();
        }

        if (Match(TokenType.Return))
        {
            return ReturnStatement();
        }
        
        if (Match(TokenType.LeftBrace))
        {
            return new Block(Block());
        }
        
        return ExpressionStatement();
    }

    private Stmt ReturnStatement()
    {
        var token = Previous();
        Expr? value = null;

        if (!Check(TokenType.SemiColon))
        {
            value = Expression();
        }

        Consume(TokenType.SemiColon, "Expect ';' after return value.");
        return new Return(token, value);
    }

    private Stmt ForStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");
        Stmt? initializer = null;

        if (Match(TokenType.SemiColon))
            initializer = null;
        else if (Match(TokenType.Var))
            initializer = VarDeclaration();
        else
            initializer = ExpressionStatement();

        Expr? condition = null;

        if (!Check(TokenType.SemiColon))
            condition = Expression();

        Consume(TokenType.SemiColon, "Expect ';' after for condition.");

        Expr? increment = null;
        
        if (!Check(TokenType.RightParen))
            increment = Expression();
        
        Consume(TokenType.RightParen, "Expect ')' after for clauses.");
        
        var body = Statement();

        if (increment != null)
        {
            body = new Block([
                body,
                new Expression(increment)
            ]);
        }

        body = new While(
            condition ?? new Literal(true),
            body
        );

        if (initializer != null)
        {
            body = new Block([
                initializer,
                body,
            ]);
        }
        
        return body;
    }

    private Stmt WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after while condition.");
        var body = Statement();
        
        return new While(condition, body);
    }

    Stmt IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        var thenBranch = Statement();
        var elseBranch = Match(TokenType.Else)
            ? Statement()
            : null;

        return new If(condition, thenBranch, elseBranch);
    }

    Stmt ExpressionStatement()
    {
        var expr = Expression();
        Consume(TokenType.SemiColon, "Expect ';' after expression");
        return new Expression(expr);
    }

    List<Stmt> Block()
    {
        var statements = new List<Stmt>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var declaration = Declaration();
            
            if (declaration != null)
                statements.Add(declaration);
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    Stmt PrintStatement()
    {
        var value = Expression();
        Consume(TokenType.SemiColon, "Expect ';' after value.");
        return new Print(value);
    }

    Expr Expression()
    {
        return Assignment();
    }

    Expr Assignment()
    {
        // Note: The original Lox grammar has assignment at a very low precedence.
        // If pipe is above assignment but below logical OR/AND, then
        // Assignment -> Or (or whatever is above pipe)
        // Or -> And
        // And -> Pipe (new)
        // Pipe -> Equality (or whatever is below pipe)
        var expr = Or(); // Or should call Pipe, which calls And. Let's adjust.
                         // Corrected chain: Assignment -> Or -> And -> Pipe -> Equality

        if (!Match(TokenType.Equal))
        {
            return expr;
        }

        var equals = Previous();
        var value = Assignment(); // Assignment is right-associative

        if (expr is Variable variable)
        {
            return new Assign(variable.Name, value);
        }
        
        Reporter.Error(equals, "Invalid assignment target.");
        return expr;
    }

    Expr Or()
    {
        var expr = And();

        while (Match(TokenType.Or))
        {
            var op = Previous();
            var right = And();
            expr = new Logical(expr, op, right);
        }

        return expr;
    }

    Expr And()
    {
        var expr = Pipe(); // And calls Pipe

        while (Match(TokenType.And))
        {
            var op = Previous();
            var right = Pipe(); // And calls Pipe
            expr = new Logical(expr, op, right);
        }

        return expr;
    }

    Expr Pipe() // New method for |> operator
    {
        var expr = Equality(); // Pipe calls Equality

        while (Match(TokenType.PipeGreater))
        {
            var op = Previous();
            var right = Equality(); // Pipe calls Equality
            expr = new Pipe(expr, op, right);
        }

        return expr;
    }

    Expr Equality()
    {
        Expr expr = Comparaison();

        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous();
            var right = Comparaison();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    Expr Comparaison()
    {
        Expr expr = Term();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous();
            var right = Term();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    Expr Term()
    {
        Expr expr = Factor();

        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous();
            var right = Factor();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    Expr Factor()
    {
        Expr expr = Unary();

        while (Match(TokenType.Slash, TokenType.Star))
        {
            var op = Previous();
            var right = Unary();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var op = Previous();
            var right = Unary();
            return new Unary(op, right);
        }

        return CallExpression();
    }

    private Expr CallExpression()
    {
        var expr = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        var arguments = new List<Expr>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= 255)
                    Error(Peek(), "Can't have more than 255 arguments.");
                
                arguments.Add(Expression());
            } while (Match(TokenType.Comma));
        }
        
        var token = Consume(TokenType.RightParen, "Expect ')' after arguments.");

        return new Call(callee, token, arguments);
    }

    Expr Primary()
    {
        if (Match(TokenType.False))
            return new Literal(false);

        if (Match(TokenType.True))
            return new Literal(true);

        if (Match(TokenType.Nil))
            return new Literal(null);

        if (Match(TokenType.Number, TokenType.String))
            return new Literal(Previous().Literal);

        if (Match(TokenType.Identifier))
            return new Variable(Previous());

        if (Match(TokenType.Dollar)) // Handling for $ placeholder
            return new Variable(Previous());
        
        if (Match(TokenType.LeftParen))
        {
            var expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Grouping(expr);
        }

        throw Error(Peek(), "Expect expression");
    }

    bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();

        throw Error(Peek(), message);
    }

    Exception Error(Token token, string message)
    {
        Reporter.Error(token, message);
        return new ParseError();
    }

    void Synchonize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.SemiColon)
                return;

            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    bool IsAtEnd()
    {
        return Peek().Type == TokenType.EOF;
    }

    Token Peek()
    {
        return _tokens.ElementAt(_current);
    }

    Token Previous()
    {
        return _tokens.ElementAt(_current - 1);
    }
}