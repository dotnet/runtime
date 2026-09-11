// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<T> jsonTypeInfo)
    /// overload is trimming safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            int[] arr = JsonSerializer.Deserialize(Encoding.UTF8.GetBytes(json), Context.Default.Int32Array);
            if (arr is not [1])
            {
                return -1;
            }

            json = """{"X":1,"Y":2}""";
            MyClassWithParameterizedCtor obj = JsonSerializer.Deserialize(Encoding.UTF8.GetBytes(json), Context.Default.MyClassWithParameterizedCtor);
            if (obj is not { X: 1, Y: 2 })
            {
                return -2;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(MyClassWithParameterizedCtor))]
    internal partial class Context : JsonSerializerContext;
}
