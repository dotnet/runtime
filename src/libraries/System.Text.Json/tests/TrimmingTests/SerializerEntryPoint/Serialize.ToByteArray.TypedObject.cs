// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations. A collection and a POCO are used. Public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            int[] arr = new [] { 1 };
            string expected = "[1]";
            string actual = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(arr));
            if (actual != expected)
            {
                return -1;
            }

            MyStruct obj = default;
            expected = @"{""X"":0,""Y"":0}";
            actual = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(obj));
            if (!TestHelper.JsonEqual(expected, actual))
            {
                return -1;
            }

            return 100;
        }
    }
}
