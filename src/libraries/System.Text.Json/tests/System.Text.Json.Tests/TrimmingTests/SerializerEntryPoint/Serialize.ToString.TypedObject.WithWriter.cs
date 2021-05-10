// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations. A collection and a POCO are used. Public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            {
                int[] arr = new [] { 1 };
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, arr);
                string actual = Encoding.UTF8.GetString(stream.ToArray());
                if (actual != "[1]")
                {
                    return -1;
                }
            }

            {
                MyStruct obj = default;
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, obj);
                string actual = Encoding.UTF8.GetString(stream.ToArray());
                if (!TestHelper.JsonEqual(@"{""X"":0,""Y"":0}", actual))
                {
                    return -1;
                }
            }

            return 100;
        }
    }
}
