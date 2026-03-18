using System;
using System.Threading.Tasks;

public class TestClass {
    static void WriteTestOutput(string output) => Console.WriteLine($"TestOutput -> {output}");

    public static int Main(string[] args)
    {
        int count = args == null ? 0 : args.Length;
        WriteTestOutput($"args#: {args?.Length}");
        foreach (var arg in args ?? Array.Empty<string>())
            WriteTestOutput($"arg: {arg}");
        return 42 + count;
    }
}
