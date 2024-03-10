using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library;

public record Person(string FirstName, string LastName);

[JsonSerializable(typeof(Person))]
public partial class PersonJsonSerializerContext : JsonSerializerContext
{
}