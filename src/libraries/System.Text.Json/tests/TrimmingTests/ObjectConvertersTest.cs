// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests the serializer (de)serializes objects appropriately,
    /// and that the object converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = @"{""X"":1,""Y"":2}";

            MyClass @class = JsonSerializer.Deserialize<MyClass>(json); // ObjectDefaultConverter
            if (@class.X != 1 || @class.Y != 2 || !TestHelper.JsonEqual(json, JsonSerializer.Serialize(@class)))
            {
                return -1;
            }

            MyStruct @struct = JsonSerializer.Deserialize<MyStruct>(json); // SmallObjectWithParameterizedConstructorConverter
            if (@struct.X != 1 || @struct.Y != 2 || !TestHelper.JsonEqual(json, JsonSerializer.Serialize(@struct)))
            {
                return -1;
            }

            json = @"{""A"":""A"",""B"":""B"",""C"":""C"",""One"":1,""Two"":2,""Three"":3}";
            MyBigClass bigClass = JsonSerializer.Deserialize<MyBigClass>(json); // LargeObjectWithParameterizedConstructorConverter
            if (bigClass.A != "A" ||
                bigClass.B != "B" ||
                bigClass.C != "C" ||
                bigClass.One != 1 ||
                bigClass.Two != 2 ||
                bigClass.Three != 3 ||
                !TestHelper.JsonEqual(json, JsonSerializer.Serialize(bigClass)))
            {
                return -1;
            }

            json = @"{""Key"":1,""Value"":2}";
            KeyValuePair<int, int> kvp = JsonSerializer.Deserialize<KeyValuePair<int, int>>(json); // KeyValuePairConverter
            if (kvp.Key != 1 || kvp.Value != 2 || !TestHelper.JsonEqual(json, JsonSerializer.Serialize(kvp)))
            {
                return -1;
            }

            return 100;
        }
    }
}
