using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public static class Program
{
    public static int Main()
    {
        string valueToSerialize = "stringValue";

        if (JsonSerializerOptions.Default.TypeInfoResolver is not IList<IJsonTypeInfoResolver> { Count: 0 })
        {
            return -1;
        }

        try
        {
            JsonSerializer.Serialize(valueToSerialize);
            return -2;
        }
        catch (NotSupportedException)
        {
        }

        var options = new JsonSerializerOptions();
        try
        {
            JsonSerializer.Serialize(valueToSerialize, options);
            return -3;
        }
        catch (InvalidOperationException)
        {
        }

        Type reflectionResolver = GetJsonType("System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver");
        if (reflectionResolver != null)
        {
            return -5;
        }

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetJsonType(string name) =>
        typeof(JsonSerializer).Assembly.GetType(name, throwOnError: false);
}
