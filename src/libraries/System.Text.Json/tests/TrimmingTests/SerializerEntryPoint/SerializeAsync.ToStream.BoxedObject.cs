// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.SerializeAsync(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options, CancellationToken cancellationToken)
    /// overload has the appropriate linker annotations. A collection and a POCO are used. Public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            using (var stream = new MemoryStream())
            {
                int[] arr = new [] { 1 };
                await JsonSerializer.SerializeAsync(stream, arr, typeof(int[]));
                string actual = Encoding.UTF8.GetString(stream.ToArray());
                if ("[1]" != actual)
                {
                    return -1;
                }
            }

            using (var stream = new MemoryStream())
            {
                MyStruct obj = default;
                await JsonSerializer.SerializeAsync(stream, obj, typeof(MyStruct));
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
