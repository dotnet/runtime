using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine($"args[{i}] = {args[i]}");
        }

        await Task.Delay(0);

        return 0;
    }
}
