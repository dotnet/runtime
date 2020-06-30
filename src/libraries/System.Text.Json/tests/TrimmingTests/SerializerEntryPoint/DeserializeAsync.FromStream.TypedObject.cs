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
    /// Tests that the serializer's JsonSerializer.DeserializeAsync<T>(Stream utf8Json, JsonSerializerOptions options, CancellationToken cancellationToken)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors and public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            string json = "[1]";
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                int[] arr = (int[])(await JsonSerializer.DeserializeAsync<int[]>(stream));
                if (!TestHelper.VerifyWithSerialize(arr, json))
                {
                    return -1;
                }
            }

            json = @"{""X"":1,""Y"":2}";
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                MyStruct obj = (MyStruct)(await JsonSerializer.DeserializeAsync<MyStruct>(stream));
                if (!TestHelper.VerifyWithSerialize(obj, json))
                {
                    return -1;
                }
            }

            return 100;
        }
    }
}
