// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize(string json, Type returnType, JsonSerializerContext context)
    /// overload is trimming-safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            int[] arr = (int[])JsonSerializer.Deserialize(json, typeof(int[]), Context.Default);
            if (arr is not [1])
            {
                Console.Error.WriteLine("Deserializing int[] returned an unexpected value.");
                return -1;
            }

            json = """{"X":1,"Y":2}""";
            var obj = (MyClassWithParameterizedCtor)JsonSerializer.Deserialize(json, typeof(MyClassWithParameterizedCtor), Context.Default);
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
