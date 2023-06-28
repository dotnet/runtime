using System;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

Console.WriteLine("Hello, Browser!");

public partial class MyClass
{
    [JSExport]
    internal static string GetJson()
    {
        var text = JsonSerializer.Serialize(new Person("John", "Doe"));
        Console.WriteLine(text);
        return text;
    }
}

public record Person(string FirstName, string LastName);