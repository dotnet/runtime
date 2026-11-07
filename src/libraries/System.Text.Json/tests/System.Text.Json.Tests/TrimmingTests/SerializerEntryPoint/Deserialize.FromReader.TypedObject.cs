// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(ref Utf8JsonReader reader, JsonTypeInfo<T> jsonTypeInfo)
    /// overload is trimming-safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
            int[] arr = JsonSerializer.Deserialize(ref reader, Context.Default.Int32Array);
            if (arr is not [1])
            {
                Console.Error.WriteLine("Deserializing int[] returned an unexpected value.");
                return -1;
            }

            json = """{"X":1,"Y":2}""";
            reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            MyClassWithParameterizedCtor obj = JsonSerializer.Deserialize(ref reader, Context.Default.MyClassWithParameterizedCtor);
            if (obj is not { X: 1, Y: 2 })
            {
                Console.Error.WriteLine("Deserializing MyClassWithParameterizedCtor returned an unexpected value.");
                return -1;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(MyClassWithParameterizedCtor))]
    internal partial class Context : JsonSerializerContext;
}
