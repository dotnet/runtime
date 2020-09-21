// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors and public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            int[] arr = JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(json));
            if (arr == null || arr.Length != 1 || arr[0] != 1)
            {
                return -1;
            }

            json = @"{""X"":1,""Y"":2}";
            MyClassWithParameterizedCtor obj = JsonSerializer.Deserialize<MyClassWithParameterizedCtor>(Encoding.UTF8.GetBytes(json));
            if (obj == null)
            {
                return -1;
            }

            return 100;
        }
    }
}
