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
    /// Tests that the serializer's JsonSerializer.DeserializeAsync(Stream utf8Json, Type returnType, JsonSerializerOptions options, CancellationToken cancellationToken)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors properties and public are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";

            int[] arr = (int[])Task.Run(async () =>
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, typeof(int[]));
                }
            }).GetAwaiter().GetResult();

            if (arr[0] != 1)
            {
                return -1;
            }

            json = @"{""X"":1,""Y"":2}";
            MyStruct obj = (MyStruct)Task.Run(async () =>
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, typeof(MyStruct));
                }
            }).GetAwaiter().GetResult();

            if (obj.X != 1 || obj.Y != 2)
            {
                return -1;
            }

            return 100;
        }
    }
}
