class Scanner
{
    private readonly string source;
    private readonly List<Token> tokens = [];

    private int start;
    private int current;
    private int line;

    private bool IsAtEnd => current >= source.Length;

    private readonly static Dictionary<string, TokenType> keywords = new()
    {
        { "and", TokenType.And },
        { "class", TokenType.Class },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "for", TokenType.For },
        { "fun", TokenType.Fun },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While}
    };

    public Scanner(string source)
    {
        this.source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd)
        {
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(TokenType.EOF, "", null, line));
        return tokens;
    }

    public void ScanToken()
    {
        char c = Advance();

        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.SemiColon); break;
            case '*': AddToken(TokenType.Star); break;
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd) Advance();
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                break;

            case '\n':
                line++;
                break;

            case '"':
                ParseString();
                break;
            case 'o':
                if (Match('r'))
                {
                    AddToken(TokenType.Or);
                }
                break;
            default:
                if (IsDigit(c))
                {
                    ParseNumber();
                    break;
                }

                if (IsAlpha(c))
                {
                    ParseIdentifier();
                    break;
                }

                Reporter.Error(line, "Unexpected character.");
                break;
        }
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }

    private void ParseIdentifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
           Advance();
        }

        var text = source.Substring(start, current - start);
        var type = TokenType.Identifier;

        if (keywords.ContainsKey(text))
        {
            type = keywords[text];
        }

        AddToken(type);
    }

    private bool IsAlphaNumeric(char c)
    {
        return IsDigit(c) || IsAlpha(c);
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private char Peek()
    {
        if (IsAtEnd)
        {
            return '\0';
        }

        return source.ElementAt(current);
    }

    private bool Match(char expected)
    {
        if (IsAtEnd)
            return false;

        if (source.ElementAt(current) != expected)
            return false;

        current++;
        return true;
    }

    private char Advance()
    {
        return source.ElementAt(current++);
    }

    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    private void AddToken(TokenType type, object? literal)
    {
        var text = source.Substring(start, current - start);
        tokens.Add(new Token(type, text, literal, line));
    }

    private void ParseString()
    {
        while (Peek() != '"' && !IsAtEnd)
        {
            if (Peek() == '\n') line++;
            Advance();
        }

        if (IsAtEnd)
        {
            Reporter.Error(line, "Unterminated string.");
            return;
        }

        Advance();
        string value = source.Substring(start + 1, current - start - 1);
        AddToken(TokenType.String, value);
    }

    private void ParseNumber()
    {
        while (IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();

            while (IsDigit(Peek()))
                Advance();
        }

        AddToken(TokenType.Number, double.Parse(source.Substring(start, current - start)));
    }

    private char PeekNext()
    {
        if (current + 1 >= source.Length)
        {
            return '\0';
        }

        return source.ElementAt(current + 1);
    }
}