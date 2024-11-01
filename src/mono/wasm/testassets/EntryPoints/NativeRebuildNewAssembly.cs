using System;
using System.Text.Json;
public class Test
{
    public static int Main()
    {
        string json = "{ \"name\": \"value\" }";
        var jdoc = JsonDocument.Parse($"{json}", new JsonDocumentOptions());
        Console.WriteLine($"json: {jdoc}");
        return 42;
    }
}