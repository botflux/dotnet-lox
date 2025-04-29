// See https://aka.ms/new-console-template for more information


using System.Text;

var arguments = Environment.GetCommandLineArgs();

// var astPrinter = new AstPrinter();

var astPrinter = new ReversePolishNotationAstPrinter();

var expr = new Binary(
    new Binary(
        new Literal(1),
        new Token(TokenType.Plus, "+", null, 1),
        new Literal(2)
    ),
    new Token(TokenType.Star, "*", null, 1),
    new Binary(
        new Literal(3),
        new Token(TokenType.Minus, "-", null, 1),
        new Literal(4)
    )
);

Console.WriteLine(astPrinter.Print(expr));

if (arguments.Length > 2)
{
    Console.WriteLine("Usage: dotnet-lox [script]");
    Environment.Exit(64);
}
else if (arguments.Length == 2)
{
    RunFile(arguments[1]);
}
else
{
    RunPrompt();
}

void RunFile(string filename)
{
    var text = File.ReadAllText(filename, Encoding.UTF8);
    Run(text);

    if (Reporter.HadError)
    {
        Environment.Exit(65);
    }
}

void RunPrompt()
{
    for (; ; )
    {
        Console.Write("> ");
        var line = Console.ReadLine();

        if (line == null) break;
        Run(line);

        if (Reporter.HadError)
        {
            Environment.Exit(65);
        }
    }
}

void Run(string source)
{
    var scanner = new Scanner(source);
    var tokens = scanner.ScanTokens();

    foreach (var token in tokens)
    {
        Console.WriteLine(token);
    }
}
