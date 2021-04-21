// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors and public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            Utf8JsonReader reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            int[] arr = (int[])JsonSerializer.Deserialize(ref reader, typeof(int[]));
            if (arr == null || arr.Length != 1 || arr[0] != 1)
            {
                return -1;
            }

            json = @"{""X"":1,""Y"":2}";
            reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            var obj = (MyClassWithParameterizedCtor)JsonSerializer.Deserialize(json, typeof(MyClassWithParameterizedCtor));
            if (obj == null)
            {
                return -1;
            }

            return 100;
        }
    }
}
