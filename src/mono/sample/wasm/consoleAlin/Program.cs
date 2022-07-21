using System;
using System.Runtime.InteropServices.JavaScript;

Console.WriteLine("Hello, Console");

public partial class MyClass
{
    [JSExport]
    internal static string Greeting()
    {
        var text = $"Hello, World! Greetings from node version: {GetNodeVersion()}";
        Console.WriteLine(text);
        return text;
    }

    [JSImport("node.process.version")]
    internal static partial string GetNodeVersion();

}
