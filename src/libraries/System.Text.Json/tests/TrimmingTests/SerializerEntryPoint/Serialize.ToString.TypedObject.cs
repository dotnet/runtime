// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations. A collection and a POCO are used. Public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            int[] arr = new [] { 1 };
            if (JsonSerializer.Serialize(arr) != "[1]")
            {
                return -1;
            }

            MyStruct obj = default;
            if (!TestHelper.JsonEqual(@"{""X"":0,""Y"":0}", JsonSerializer.Serialize(obj)))
            {
                return -1;
            }

            return 100;
        }
    }
}
