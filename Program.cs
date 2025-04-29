// See https://aka.ms/new-console-template for more information


using System.Text;

var arguments = Environment.GetCommandLineArgs();

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
