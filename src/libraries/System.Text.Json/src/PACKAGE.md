## About

<!-- A description of the package and where one can find more documentation -->

Provides high-performance and low-allocating types that serialize objects to JavaScript Object Notation (JSON) text and deserialize JSON text to objects, with UTF-8 support built-in. Also provides types to read and write JSON text encoded as UTF-8, and to create an in-memory document object model (DOM), that is read-only, for random access of the JSON elements within a structured view of the data.

## Key Features

<!-- The key features of this package -->

* High-performance reader and writer types for UTF-8 encoded JSON.
* A fully-featured JSON serializer for .NET types using reflection or source generated contracts.
* A high-performance read-only JSON DOM (JsonDocument) and a mutable DOM that interoperates with the serializer (JsonNode).
* Built-in support for async serialization, including IAsyncEnumerable support.
* Fully customizable contract model for serializable types.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The System.Text.Json library is built-in as part of the shared framework in .NET Runtime. The package can be installed when you need to use the most recent version in older target frameworks.

Serialization:
```csharp
using System;
using System.Text.Json;

WeatherForecast forecast = new (DateTimeOffset.Now, 26.6f, "Sunny");
var serialized = JsonSerializer.Serialize(forecast);

Console.WriteLine(serialized);
// {"Date":"2023-08-02T16:01:20.9025406+00:00","TemperatureCelsius":26.6,"Summary":"Sunny"}

var forecastDeserialized = JsonSerializer.Deserialize<WeatherForecast>(serialized);
Console.WriteLine(forecast == forecastDeserialized);
// True

public record WeatherForecast(DateTimeOffset Date, float TemperatureCelsius, string? Summary);
```

Serialization using the source generator:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

WeatherForecast forecast = new (DateTimeOffset.Now, 26.6f, "Sunny");
var serialized = JsonSerializer.Serialize(forecast, SourceGenerationContext.Default.WeatherForecast);

Console.WriteLine(serialized);
// {"Date":"2023-08-02T16:01:20.9025406+00:00","TemperatureCelsius":26.6,"Summary":"Sunny"}

var forecastDeserialized = JsonSerializer.Deserialize<WeatherForecast>(serialized, SourceGenerationContext.Default.WeatherForecast);
Console.WriteLine(forecast == forecastDeserialized);
// True

public record WeatherForecast(DateTimeOffset Date, float TemperatureCelsius, string? Summary);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WeatherForecast))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
```

Using the JSON DOM:
```csharp

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

string jsonString =
@"{
  ""Date"": ""2019-08-01T00:00:00"",
  ""Temperature"": 25,
  ""Summary"": ""Hot"",
  ""DatesAvailable"": [
    ""2019-08-01T00:00:00"",
    ""2019-08-02T00:00:00""
  ],
  ""TemperatureRanges"": {
      ""Cold"": {
          ""High"": 20,
          ""Low"": -10
      },
      ""Hot"": {
          ""High"": 60,
          ""Low"": 20
      }
  }
}
";

JsonNode forecastNode = JsonNode.Parse(jsonString)!;


// Get value from a JsonNode.
JsonNode temperatureNode = forecastNode["Temperature"]!;
Console.WriteLine($"Type={temperatureNode.GetType()}");
Console.WriteLine($"JSON={temperatureNode.ToJsonString()}");
//output:
//Type = System.Text.Json.Nodes.JsonValue`1[System.Text.Json.JsonElement]
//JSON = 25

// Get a typed value from a JsonNode.
int temperatureInt = (int)forecastNode["Temperature"]!;
Console.WriteLine($"Value={temperatureInt}");
//output:
//Value=25

// Get a typed value from a JsonNode by using GetValue<T>.
temperatureInt = forecastNode["Temperature"]!.GetValue<int>();
Console.WriteLine($"TemperatureInt={temperatureInt}");
//output:
//Value=25

// Get a JSON object from a JsonNode.
JsonNode temperatureRanges = forecastNode["TemperatureRanges"]!;
Console.WriteLine($"Type={temperatureRanges.GetType()}");
Console.WriteLine($"JSON={temperatureRanges.ToJsonString()}");
//output:
//Type = System.Text.Json.Nodes.JsonObject
//JSON = { "Cold":{ "High":20,"Low":-10},"Hot":{ "High":60,"Low":20} }

// Get a JSON array from a JsonNode.
JsonNode datesAvailable = forecastNode["DatesAvailable"]!;
Console.WriteLine($"Type={datesAvailable.GetType()}");
Console.WriteLine($"JSON={datesAvailable.ToJsonString()}");
//output:
//datesAvailable Type = System.Text.Json.Nodes.JsonArray
//datesAvailable JSON =["2019-08-01T00:00:00", "2019-08-02T00:00:00"]

// Get an array element value from a JsonArray.
JsonNode firstDateAvailable = datesAvailable[0]!;
Console.WriteLine($"Type={firstDateAvailable.GetType()}");
Console.WriteLine($"JSON={firstDateAvailable.ToJsonString()}");
//output:
//Type = System.Text.Json.Nodes.JsonValue`1[System.Text.Json.JsonElement]
//JSON = "2019-08-01T00:00:00"

// Get a typed value by chaining references.
int coldHighTemperature = (int)forecastNode["TemperatureRanges"]!["Cold"]!["High"]!;
Console.WriteLine($"TemperatureRanges.Cold.High={coldHighTemperature}");
//output:
//TemperatureRanges.Cold.High = 20

// Parse a JSON array
JsonNode datesNode = JsonNode.Parse(@"[""2019-08-01T00:00:00"",""2019-08-02T00:00:00""]")!;
JsonNode firstDate = datesNode[0]!.GetValue<DateTime>();
Console.WriteLine($"firstDate={ firstDate}");
//output:
//firstDate = "2019-08-01T00:00:00"
```

Using the low-level JSON reader/writer types
```csharp
using System;
using System.IO;
using System.Text;
using System.Text.Json;

var writerOptions = new JsonWriterOptions
{
    Indented = true
};

using var stream = new MemoryStream();
using var writer = new Utf8JsonWriter(stream, writerOptions);

writer.WriteStartObject();
writer.WriteString("date", DateTimeOffset.Parse("8/2/2023 9:00 AM"));
writer.WriteNumber("temp", 42);
writer.WriteEndObject();
writer.Flush();

var jsonBytes = stream.ToArray();
string json = Encoding.UTF8.GetString(jsonBytes);
Console.WriteLine(json);
// {
//   "date": "2023-08-02T09:00:00+00:00"
//   "temp": 42
// }

var readerOptions = new JsonReaderOptions
{
    AllowTrailingCommas = true,
    CommentHandling = JsonCommentHandling.Skip
};
var reader = new Utf8JsonReader(jsonBytes, readerOptions);

while (reader.Read())
{
    Console.Write(reader.TokenType);

    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName:
        case JsonTokenType.String:
            {
                string? text = reader.GetString();
                Console.Write(" ");
                Console.Write(text);
                break;
            }

        case JsonTokenType.Number:
            {
                int intValue = reader.GetInt32();
                Console.Write(" ");
                Console.Write(intValue);
                break;
            }

            // Other token types elided for brevity
    }
    Console.WriteLine();
}
// StartObject
// PropertyName date
// String 2023-08-02T09:00:00+00:00
// PropertyName temp
// Number 42
// EndObject
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Text.Json.Utf8JsonWriter`
* `System.Text.Json.Utf8JsonReader`
* `System.Text.Json.JsonSerializer`
* `System.Text.Json.JsonConverter`
* `System.Text.Json.JsonDocument`
* `System.Text.Json.Nodes.JsonNode`
* `System.Text.Json.Serialization.Metadata.JsonTypeInfo`

## Additional Documentation

* [Conceptual documentation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.text.json)

## Related Packages

<!-- The related packages associated with this package -->

* Lightweight data formats abstraction: [System.Memory.Data](https://www.nuget.org/packages/System.Memory.Data/)
* Serialization of HttpContent: [System.Net.Http.Json](https://www.nuget.org/packages/System.Net.Http.Json/)


## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Text.Json is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
