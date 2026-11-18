// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace SerializerTrimmingTest;

internal static class Program
{
    public static int Main()
    {
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            return -1;
        }

        const string DictionaryJson = """{"2":"two","1":"first","01":"last"}""";
        ConverterHooksContext context = ConverterHooksContext.Default;

        ImmutableSortedDictionary<int, string> sorted =
            JsonSerializer.Deserialize(DictionaryJson, context.SortedDictionaryInfo)!;
        if (sorted.Count != 2 || sorted[1] != "last" || sorted[2] != "two")
        {
            return -2;
        }

        ImmutableDictionary<int, string> dictionary =
            JsonSerializer.Deserialize(DictionaryJson, context.DictionaryInfo)!;
        if (dictionary.Count != 2 || dictionary[1] != "last" || dictionary[2] != "two")
        {
            return -3;
        }

        ImmutableList<int> list = JsonSerializer.Deserialize("[1,2,3]", context.ListInfo)!;
        if (list.Count != 3 || list[0] != 1 || list[1] != 2 || list[2] != 3)
        {
            return -4;
        }

        ImmutableArray<int> array = JsonSerializer.Deserialize("[1,2,3]", context.ArrayInfo);
        if (array.Length != 3 || array[0] != 1 || array[1] != 2 || array[2] != 3)
        {
            return -5;
        }

        Queue queue = JsonSerializer.Deserialize("[1,2,3]", context.QueueInfo)!;
        if (queue.Count != 3 || ((JsonElement)queue.Dequeue()!).GetInt32() != 1)
        {
            return -6;
        }

        Stack stack = JsonSerializer.Deserialize("[1,2,3]", context.StackInfo)!;
        if (stack.Count != 3 || ((JsonElement)stack.Pop()!).GetInt32() != 3)
        {
            return -7;
        }

        FiveParameterValue value =
            JsonSerializer.Deserialize("""{"A":1,"B":2,"C":3,"D":4,"E":5}""", context.ValueInfo)!;
        if (value.A != 1 || value.B != 2 || value.C != 3 || value.D != 4 || value.E != 5)
        {
            return -8;
        }

        ConverterHooksContext disallowDuplicates = new(new JsonSerializerOptions { AllowDuplicateProperties = false });
        try
        {
            JsonSerializer.Deserialize(DictionaryJson, disallowDuplicates.SortedDictionaryInfo);
            return -9;
        }
        catch (JsonException)
        {
        }

        try
        {
            JsonSerializer.Deserialize(DictionaryJson, disallowDuplicates.DictionaryInfo);
            return -10;
        }
        catch (JsonException)
        {
        }

        return 100;
    }
}

internal sealed class FiveParameterValue
{
    public FiveParameterValue(int a, int b, int c, int d, int e)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
    }

    public int A { get; }
    public int B { get; }
    public int C { get; }
    public int D { get; }
    public int E { get; }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ImmutableSortedDictionary<int, string>), TypeInfoPropertyName = "SortedDictionaryInfo")]
[JsonSerializable(typeof(ImmutableDictionary<int, string>), TypeInfoPropertyName = "DictionaryInfo")]
[JsonSerializable(typeof(ImmutableList<int>), TypeInfoPropertyName = "ListInfo")]
[JsonSerializable(typeof(ImmutableArray<int>), TypeInfoPropertyName = "ArrayInfo")]
[JsonSerializable(typeof(Queue), TypeInfoPropertyName = "QueueInfo")]
[JsonSerializable(typeof(Stack), TypeInfoPropertyName = "StackInfo")]
[JsonSerializable(typeof(FiveParameterValue), TypeInfoPropertyName = "ValueInfo")]
internal sealed partial class ConverterHooksContext : JsonSerializerContext
{
}
