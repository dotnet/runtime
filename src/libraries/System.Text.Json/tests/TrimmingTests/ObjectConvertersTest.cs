// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the object converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = @"{""X"":1,""Y"":2}";

            MyClass @class = JsonSerializer.Deserialize<MyClass>(json); // ObjectDefaultConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(@class)))
            {
                return -1;
            }

            MyStruct @struct = JsonSerializer.Deserialize<MyStruct>(json); // SmallObjectWithParameterizedConstructorConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(@struct)))
            {
                return -1;
            }

            json = @"{""A"":""A"",""B"":""B"",""C"":""C"",""One"":1,""Two"":2,""Three"":3}";
            MyBigClass bigClass = JsonSerializer.Deserialize<MyBigClass>(json); // LargeObjectWithParameterizedConstructorConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(bigClass)))
            {
                return -1;
            }

            json = @"{""Key"":1,""Value"":2}";
            KeyValuePair<int, int> kvp = JsonSerializer.Deserialize<KeyValuePair<int, int>>(json); // KeyValuePairConverter
            if (!TestHelper.JsonEqual(json, JsonSerializer.Serialize(kvp)))
            {
                return -1;
            }

            return 100;
        }
    }
}
