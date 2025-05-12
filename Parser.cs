using System.Linq.Expressions;
using System.Text.RegularExpressions;

class Parser
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
        if (Match(TokenType.Print))
        {
            return PrintStatement();
        }

        if (Match(TokenType.LeftBrace))
        {
            return new Block(Block());
        }
        
        return ExpressionStatement();
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
        var expr = Equality();

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