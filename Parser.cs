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
            var d = Declaration(false);

            if (d != null)
            {
                statements.Add(d);
            }
        }

        return statements;
    }

    Stmt? Declaration(bool isParsingLoop)
    {
        try
        {
            if (Match(TokenType.Var))
            {
                return VarDeclaration();
            }

            return Statement(isParsingLoop);
        }
        catch (ParseError)
        {
            Synchonize();
            return null;
        }
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
    
    Stmt Statement(bool isParsingLoop)
    {
        if(Match(TokenType.Break)) 
        {
            if (isParsingLoop)
            {
                return BreakStatement();    
            }
            
            throw Error(Peek(), "Cannot declare a break statement outside a loop.");
        }

        if (Match(TokenType.Continue))
        {
            if (isParsingLoop)
            {
                return ContinueStatement();
            }
            
            throw Error(Peek(), "Cannot declare a continue statement outside a loop.");
        }
        
        if (Match(TokenType.For))
        {
            return ForStatement();
        }
        
        if (Match(TokenType.If))
        {
            return IfStatement(isParsingLoop);
        }

        if (Match(TokenType.While))
        {
            return WhileStatement();
        }
        
        if (Match(TokenType.Print))
        {
            return PrintStatement();
        }

        if (Match(TokenType.LeftBrace))
        {
            return new Block(Block(isParsingLoop));
        }
        
        return ExpressionStatement();
    }

    private Stmt ContinueStatement()
    {
        Consume(TokenType.SemiColon, "Expect ';' after continue.");
        return new Continue();
    }

    private Stmt BreakStatement()
    {
        Consume(TokenType.SemiColon, "Expect ';' after break.");
        return new Break();
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
        
        var body = Statement(true);

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
        var body = Statement(true);
        
        return new While(condition, body);
    }

    Stmt IfStatement(bool isParsingLoop)
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        var thenBranch = Statement(isParsingLoop);
        var elseBranch = Match(TokenType.Else)
            ? Statement(isParsingLoop)
            : null;

        return new If(condition, thenBranch, elseBranch);
    }

    Stmt ExpressionStatement()
    {
        var expr = Expression();
        Consume(TokenType.SemiColon, "Expect ';' after expression");
        return new Expression(expr);
    }

    List<Stmt> Block(bool isParsingLoop)
    {
        var statements = new List<Stmt>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var declaration = Declaration(isParsingLoop);
            
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
        var expr = Equality();

        while (Match(TokenType.And))
        {
            var op = Previous();
            var right = Equality();
            expr = new Logical(expr, op, right);
        }

        return expr;
    }
    
    Expr Assignment()
    {
        var expr = Or();

        if (!Match(TokenType.Equal))
        {
            return expr;
        }

        var equals = Previous();
        var value = Assignment();

        if (expr is Variable variable)
        {
            return new Assign(variable.Name, value);
        }
        
        Reporter.Error(equals, "Invalid assignment target.");
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

        return Primary();
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
        return Peek().Type == TokenType.Eof;
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