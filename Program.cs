// See https://aka.ms/new-console-template for more information


using System.Text;
using dotnet_lox;

var arguments = Environment.GetCommandLineArgs();
var interpreter = new Interpreter();

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

    if (Reporter.HadRuntimeError)
    {
        Environment.Exit(70);
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
    var parser = new Parser(tokens);
    var statements = parser.Parse(); // Renamed from expression for clarity

    if (Reporter.HadError)
        return;

    // Removed null check for 'statements' as parser.Parse() returns List<Stmt> (can be empty, not null)

    var desugarer = new Desugarer();
    var desugaredStatements = desugarer.Desugar(statements);

    if (Reporter.HadError) // Check for errors from desugaring (e.g., missing '$')
        return;

    interpreter.Interpret(desugaredStatements);
}
