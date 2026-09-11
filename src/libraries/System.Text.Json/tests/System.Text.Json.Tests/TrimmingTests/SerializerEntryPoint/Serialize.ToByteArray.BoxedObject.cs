// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.SerializeToUtf8Bytes(object value, Type inputType,
    /// JsonSerializerContext context) overload is trimming safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            int[] arr = [1];
            string actual = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(arr, typeof(int[]), Context.Default));
            if (actual != "[1]")
            {
                return -1;
            }

            MyStruct obj = default;
            actual = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(obj, typeof(MyStruct), Context.Default));
            if (!TestHelper.JsonEqual("""{"X":0,"Y":0}""", actual))
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
