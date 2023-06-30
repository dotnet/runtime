using System;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class LazyLoadingTest
{
    [JSExport]
    public static void Run()
    {
        var text = JsonSerializer.Serialize(new Person("John", "Doe"));
        TestOutput.WriteLine(text);
    }

    public record Person(string FirstName, string LastName);
}
