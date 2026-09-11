// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Serialize(object value, Type inputType, JsonSerializerContext context)
    /// overload is trimming safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            int[] arr = [1];
            if (JsonSerializer.Serialize(arr, typeof(int[]), Context.Default) != "[1]")
            {
                return -1;
            }

            MyStruct obj = default;
            if (!TestHelper.JsonEqual("""{"X":0,"Y":0}""", JsonSerializer.Serialize(obj, typeof(MyStruct), Context.Default)))
            {
                return -2;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(MyStruct))]
    internal partial class Context : JsonSerializerContext;
}
