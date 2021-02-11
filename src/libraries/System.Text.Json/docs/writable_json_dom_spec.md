# Writable JSON Document Object Model (DOM) for `System.Text.Json`

## Introduction

`JsonNode` is a modifiable, dictionary-backed API to complement the readonly `JsonDocument`.

It is the base class for the following concrete types representing all possible kinds of JSON nodes:
* `JsonString` - representing JSON text value
* `JsonBoolean` - representing JSON boolean value (`true` or `false`)
* `JsonNumber` - representing JSON numeric value, can be created from and converted to all possible built-in numeric types
* `JsonNull` - representing JSON null value
* `JsonArray` - representing the array of JSON nodes
* `JsonObject` - representing the set of properties - named JSON nodes

It is a summer internship project being developed by @kasiabulat.

## Goals

The user should be able to:
* Build up a structured in-memory representation of the JSON payload.
* Query the document object model.
* Modify it. That includes, remove, add, and update. This means we want to build a modifiable JsonDocument analogue that is not just readonly.

## TODOs

* Designing API
* Implementation of provided methods
* Tests of provided methods
* Documentation for public API

## Example scenarios
### Collection initialization

One of the aims in designing this API was to take advantage of C# language features and make it easy and natural for delevopers to create instances of `JsonObjects` without calling too many `new` instructions. Below example shows how to initialize JSON object with different types of properties:

```csharp
var developer = new JsonObject
{
    { "name", "Kasia" },
    { "age", 22 },
    { "is developer", true },
    { "null property", (JsonNode) null }
};
```

JSON object can be nested within other JSON objects or include a JSON array:

```csharp
var person = new JsonObject
{
    { "name", "John" },
    { "surname", "Smith" },
    {
        "addresses", new JsonObject()
        {
            {
                "office", new JsonObject()
                {
                    {  "address line 1", "One Microsoft Way" },
                    {  "city" , "Redmond" } ,
                    {  "zip code" , 98052 } ,
                    {  "state" , (int) AvailableStateCodes.WA }
                }
            },
            {
                "home", new JsonObject()
                {
                    {  "address line 1", "Pear Ave" },
                    {  "address line 2", "1288" },
                    {  "city" , "Mountain View" } ,
                    {  "zip code" , 94043 } ,
                    {  "state" , (int) AvailableStateCodes.CA }
                }
            }
        }
    },
    {
        "phone numbers", new JsonArray()
        {
            "123-456-7890",
            "123-456-7890"
        }
    }
};
```

JSON array can be also initialized easily in various ways which might be useful in different scenarios:

```csharp
string[] dishes = { "sushi", "pasta", "cucumber soup" };
IEnumerable<string> sports = sportsExperienceYears.Where(sport => ((JsonNumber)sport.Value).GetInt32() > 2).Select(sport => sport.Key);

var preferences = new JsonObject()
{
    { "colours", new JsonArray { "red", "green", "purple" } },
    { "numbers", new JsonArray { 4, 123, 88 } },
    { "varia", new JsonArray { 1, "value", false } },
    { "dishes", new JsonArray(dishes) },
    { "sports", new JsonArray(sports) }
};
```

### Modifying existing instance

The main goal of the new API is to allow users to modify existing instance of `JsonNode` which is not possible with `JsonElement` and `JsonDocument`.

One may change the existing property to have a different value:
```csharp
 var options = new JsonObject { { "use caching", true } };
 options["use caching"] = false;
```

Add a value to existing JSON array or property to existing JSON object:
```csharp
var bestEmployees = new JsonObject(EmployeesDatabase.GetTenBestEmployees());
bestEmployees.Add("manager", EmployeesDatabase.GetManager());


var employeesIds = new JsonArray();
foreach (KeyValuePair<string, JsonNode> employee in EmployeesDatabase.GetTenBestEmployees())
{
    employeesIds.Add(employee.Key);
}
```

And easily access nested objects:
```csharp
var issues = new JsonObject()
{
    { "features", new JsonArray{ "new functionality 1", "new functionality 2" } },
    { "bugs", new JsonArray{ "bug 123", "bug 4566", "bug 821" } },
    { "tests", new JsonArray{ "code coverage" } },
};

issues.GetJsonArrayProperty("bugs").Add("bug 12356");
issues.GetJsonArrayProperty("features")[0] = "feature 1569";
```

### Transforming to and from JsonElement

The API allows users to get a writable version of JSON document from a readonly one and vice versa:

Transforming JsonNode to JsonElement:
```csharp
JsonNode employeeDataToSend = EmployeesDatabase.GetNextEmployee().Value;
Mailbox.SendEmployeeData(employeeDataToSend.AsJsonElement());
```

Transforming JsonElement to JsonNode:
```csharp
JsonNode receivedEmployeeData = JsonNode.DeepCopy(Mailbox.RetrieveMutableEmployeeData());
if (receivedEmployeeData is JsonObject employee)
{
    employee["name"] = new JsonString("Bob");
}
```

### Parsing to JsonNode

If a developer knows they will be modifying an instance, there is an API to parse string right to `JsonNode`, without `JsonDocument` being an intermediary.

```csharp
string jsonString = @"
{
    ""employee1"" :
    {
        ""name"" : ""Ann"",
        ""surname"" : ""Predictable"",
        ""age"" : 30,
    },
    ""employee2"" :
    {
        ""name"" : ""Zoe"",
        ""surname"" : ""Coder"",
        ""age"" : 24,
    }
}";

JsonObject employees = JsonNode.Parse(jsonString) as JsonObject;

var newEmployee = new JsonObject({"name", "Bob"});
int nextId = employees.PropertyNames.Count + 1;

employees.Add("employee"+nextId.ToString(), newEmployee);
Mailbox.SendAllEmployeesData(employees.AsJsonElement());
```

## Design choices

* Avoid any significant perf regression to the readonly implementation of `JsonDocument` and `JsonElement`.
* Higher emphasis on usability over allocations/performance.
* No advanced methods for looking up properties like `GetAllValuesByPropertyName` or `GetAllPrimaryTypedValues`, because they would be too specialized.
* Support for LINQ style quering capability.
* `JsonNull` class instead of `null` reference to node.
* No additional overloads of Add methods for primary types (bool, string, int, double,  long...) for `JsonObject` and `JsonArray`. Instead - implicit cast operators in JsonNode.
* `Sort` not implemented for `JsonArray`, beacuse there is no right way to compare `JsonObjects`. If a user wants to sort a `JsonArray` of `JsonNumbers`, `JsonBooleans` or `JsonStrings` they now needs to do the following: convert the `JsonArray` to a regular array (by iterating through all elements), call sort (and convert back to `JsonArray` if needed).
* Property names duplicates handling method possible to choose during parsing to `JsonNode`. When creating `JsonObject` Add method throws an exception for duplicates and indexer replaces old property value with new one.
* No support for escaped characters when creating `JsonNumber` from string.
* `JsonValueKind` property that a caller can inspect and cast to the right concrete type
* Transformation API:
    * `DeepCopy` method allowing to change JsonElement into JsonNode recursively transforming all of the elements.
    * `AsJsonElement`method allowing to change JsonNode into JsonElement with IsImmutable property set to false.
    * `IsImmutable` property informing if JsonElement is keeping JsonDocument or JsonNode underneath.
    * `Parse(string)` method to be able to parse a JSON string right into JsonNode if the user knows they want a mutable version. It allows choosing duplicates handling method.
    * `Clone` method to make a copy of the whole JsonNode tree.
    * `GetNode` and `TryGetNode` methods allowing to retrieve it from JsonElement.
    * Internal `WriteTo(Utf8JsonWriter)` method for writing a JsonNode to a Utf8JsonWriter without having to go through JsonElement.
    * `ToJsonString` method transforming JsonNode to string representation using WriteTo.
* No recursive equals for `JsonArray` and `JsonObject`.
* `JsonNode` derived types do not implement `IComparable`.
* `JsonObject` does not implement `IDictionary`, but `JsonArray` implements `IList`.
* We support order preservation when adding/removing values in `JsonArray`/`JsonObject`.
* We do not support creating `JsonNumber` from `BigInterger` without changing it to string.
* `ToString` returns:
    * Unescaped string for `JsonString`.
    * String representation of number for `JsonNumber`.
    * "true" / "false" (JSON representation) for `JsonBoolean`.
    * Is not overloaded for `JsonArray` and `JsonObject`.

## Open questions
* Do we want `JsonArray` to support `Contains`, `IndexOf` and `LastIndexOf` if we keep reference equality for `JsonArray`/`JsonObject` and don't have a good way of comparing numbers?
* Should nodes track their own position in the JSON graph? Do we want to allow properties like Parent, Next and Previous?

    | Solution | Pros | Cons |
    |----------|:-------------|--------|
    |current API| - no additional checks need to be made | - creating recursive loop by the user may be problematic |
    |tracking nodes | - handles recursive loop problem | - when node is added to a parent, it needs to be checked <br>  if it already has a parent and make a copy if it has |

* Do we want to change JsonNumber's backing field to something different than string?

    Suggestions:
    - `Span<byte>` or array of `Utf8String`/`Char8` (once they come online in the future) / `byte`
    - Internal types that are specific to each numeric type in .NET with factories to create JsonNumber
    - Internal struct field which has all the supported numeric types
    - Unsigned long field accompanying string to store types that are <= 8 bytes long

* Should we add overloads for all nullable types as well? For example:
    ```csharp
    public static implicit operator System.Text.Json.JsonNode (bool? value) { throw null; }
    ```

* Do we want to have implicit cast operators on `JsonNull`, `JsonBoolean`, `JsonString` and `JsonNumber` while we already have them in `JsonNode`? It would be consistent, but implicit cast from e.g. float.Infinity to `JsonNumber` would throw an exception, because we would not be able return `JsonString` in this case anymore.

## Useful links

### JSON
* grammar: https://www.json.org/
* specification: https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
* RFC: https://tools.ietf.org/html/rfc8259

### Similar APIs
`JsonElement` and `JsonDocument` from `System.Text.Json` API:
* video: https://channel9.msdn.com/Shows/On-NET/Try-the-new-SystemTextJson-APIs
* blogpost: https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/

`Json.NET` and its advantages:
* XPath: https://goessner.net/articles/JsonPath/
* LINQ: https://www.newtonsoft.com/json/help/html/LINQtoJSON.htm
* XML: https://www.newtonsoft.com/json/help/html/ConvertJsonToXml.htm
* JToken: https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JToken.htm
