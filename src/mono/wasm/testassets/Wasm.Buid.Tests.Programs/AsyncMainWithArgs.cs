using System;
using System.Threading.Tasks;

public class TestClass {
    public static async Task<int> Main(string[] args)
    {
        int count = args == null ? 0 : args.Length;
        Console.WriteLine($"args#: {args?.Length}");
        foreach (var arg in args ?? Array.Empty<string>())
            Console.WriteLine($"arg: {arg}");
        return await Task.FromResult(42 + count);
    }
}