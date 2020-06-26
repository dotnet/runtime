// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        static int Main(string[] args)
        {
            {
                int[] arr = new [] { 1 };

                string actual = Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(stream, arr, typeof(int[]));
                    return Encoding.UTF8.GetString(stream.ToArray());
                }).GetAwaiter().GetResult();

                if ("[1]" != actual)
                {
                    return -1;
                }
            }

            {
                MyStruct obj = new MyStruct(1, 2);

                string actual = Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(stream, obj, typeof(MyStruct));
                    return Encoding.UTF8.GetString(stream.ToArray());
                }).GetAwaiter().GetResult();

                if (!TestHelper.JsonEqual(@"{""X"":1,""Y"":2}", actual))
                {
                    return -1;
                }
            }

            return 100;
        }
    }
}
