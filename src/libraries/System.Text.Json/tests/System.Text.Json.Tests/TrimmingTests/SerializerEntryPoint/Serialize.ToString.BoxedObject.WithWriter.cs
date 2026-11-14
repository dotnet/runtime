// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Serialize(Utf8JsonWriter writer, object value, Type inputType,
    /// JsonSerializerContext context) overload is trimming-safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            {
                using MemoryStream stream = new();
                using Utf8JsonWriter writer = new(stream);
                int[] arr = [1];
                JsonSerializer.Serialize(writer, arr, typeof(int[]), Context.Default);
                string actual = Encoding.UTF8.GetString(stream.ToArray());
                if (actual != "[1]")
                {
                    Console.Error.WriteLine("Serializing int[] produced unexpected JSON.");
                    return -1;
                }
            }

            {
                using MemoryStream stream = new();
                using Utf8JsonWriter writer = new(stream);
                MyStruct obj = default;
                JsonSerializer.Serialize(writer, obj, typeof(MyStruct), Context.Default);
                string actual = Encoding.UTF8.GetString(stream.ToArray());
                if (!TestHelper.JsonEqual("""{"X":0,"Y":0}""", actual))
                {
                    Console.Error.WriteLine("Serializing MyStruct produced unexpected JSON.");
                    return -1;
                }
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(MyStruct))]
    internal partial class Context : JsonSerializerContext;
}
