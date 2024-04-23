using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

Console.WriteLine("Hello, Browser!");

partial class Interop
{
    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();
    
    [JSExport]
    internal static string Greeting()
    {
        var text = $"Hello, World! Greetings from {GetHRef()}";
        Console.WriteLine(text);
        return text;
    }
}
