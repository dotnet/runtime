// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(string json, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors and public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            int[] arr = JsonSerializer.Deserialize<int[]>(json);
            if (!TestHelper.VerifyWithSerialize(arr, json))
            {
                return -1;
            }

            json = @"{""X"":1,""Y"":2}";
            MyStruct obj = JsonSerializer.Deserialize<MyStruct>(json);
            if (!TestHelper.VerifyWithSerialize(obj, json))
            {
                return -1;
            }

            return 100;
        }
    }
}
