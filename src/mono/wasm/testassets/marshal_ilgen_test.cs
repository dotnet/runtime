using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

Console.WriteLine("Hello, Console!");

return 0;

public partial class MyClass
{
    [JSExport]
    internal static string Greeting()
    {
        int[] x = new int[0];
        call_needing_marhsal_ilgen(x);

        var text = $"Hello, World! Greetings from node version: {GetNodeVersion()}";
        return text;
    }

    [JSImport("node.process.version", "main.mjs")]
    internal static partial string GetNodeVersion();

    [DllImport("incompatible_type")]
    static extern void call_needing_marhsal_ilgen(int[] numbers);
}

