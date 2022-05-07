## About

Provides high-performance and low-allocating types that serialize objects to JavaScript Object Notation (JSON) text and deserialize JSON text to objects, with UTF-8 support built-in. Also provides types to read and write JSON text encoded as UTF-8, and to create an in-memory document object model (DOM), that is read-only, for random access of the JSON elements within a structured view of the data.

The `System.Text.Json` library is built-in as part of the shared framework for .NET Core 3.0+ and .NET 5+. You only need to install it as a package for earlier .NET Core versions or other target frameworks.

For more informations, see [JSON serialization and deserialization in .NET](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-overview).

## Example

The following example shows how to serialize and deserialize JSON.

```
using System;
using System.Text.Json;

class Person
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public DateTime BirthDate { get; set; }        
}

class Program
{
    static void Main()
    {
        var person = new Person();
        person.Name = "John";
        person.Surname = "Smith";
        person.BirthDate = new DateTime(1988, 4, 20);

        // Serialize object to JSON:
        string jsonString = JsonSerializer.Serialize(person);
        Console.WriteLine(jsonString);
        // Output:
        // {"Name":"John","Surname":"Smith","BirthDate":"1988-04-20T00:00:00"}

        // Deserialize object from JSON:
        Person personDeserialized = JsonSerializer.Deserialize<Person>(jsonString);
        Console.WriteLine($"Name: {personDeserialized.Name}");
        Console.WriteLine($"Surname: {personDeserialized.Surname}");
        Console.WriteLine($"BirthDate: {personDeserialized.BirthDate}");
    }
}
```

## Commonly Used Types

- [System.Text.Json.JsonSerializer](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer)
- [System.Text.Json.JsonDocument](https://docs.microsoft.com/dotnet/api/system.text.json.jsondocument)
- [System.Text.Json.JsonElement](https://docs.microsoft.com/dotnet/api/system.text.json.jsonelement)
- [System.Text.Json.Utf8JsonWriter](https://docs.microsoft.com/dotnet/api/system.text.json.utf8jsonwriter)
- [System.Text.Json.Utf8JsonReader](https://docs.microsoft.com/dotnet/api/system.text.json.utf8jsonreader)
