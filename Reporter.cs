using System.Dynamic;

abstract class Reporter
{
    public static bool HadError { get; private set; }

    public static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
        HadError = true;
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }
}