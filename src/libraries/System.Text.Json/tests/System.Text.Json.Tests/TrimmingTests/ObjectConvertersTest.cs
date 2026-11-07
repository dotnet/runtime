// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the object converters are trimming-safe when used with source generated metadata.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = """{"X":1,"Y":2}""";

            MyClass @class = JsonSerializer.Deserialize(json, Context.Default.MyClass); // ObjectDefaultConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(@class, Context.Default.MyClass)))
            {
                Console.Error.WriteLine("MyClass did not round trip (ObjectDefaultConverter).");
                return -1;
            }

            MyStruct @struct = JsonSerializer.Deserialize(json, Context.Default.MyStruct); // SmallObjectWithParameterizedConstructorConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(@struct, Context.Default.MyStruct)))
            {
                Console.Error.WriteLine("MyStruct did not round trip (SmallObjectWithParameterizedConstructorConverter).");
                return -1;
            }

            json = """{"A":"A","B":"B","C":"C","One":1,"Two":2,"Three":3}""";
            MyBigClass bigClass = JsonSerializer.Deserialize(json, Context.Default.MyBigClass); // LargeObjectWithParameterizedConstructorConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(bigClass, Context.Default.MyBigClass)))
            {
                Console.Error.WriteLine("MyBigClass did not round trip (LargeObjectWithParameterizedConstructorConverter).");
                return -1;
            }

            json = """{"Key":1,"Value":2}""";
            KeyValuePair<int, int> kvp = JsonSerializer.Deserialize(json, Context.Default.KeyValuePairInt32Int32); // KeyValuePairConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(kvp, Context.Default.KeyValuePairInt32Int32)))
            {
                Console.Error.WriteLine("KeyValuePair<int, int> did not round trip (KeyValuePairConverter).");
                return -1;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(MyClass))]
    [JsonSerializable(typeof(MyStruct))]
    [JsonSerializable(typeof(MyBigClass))]
    [JsonSerializable(typeof(KeyValuePair<int, int>))]
    internal partial class Context : JsonSerializerContext;
}
