// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's warm up routine for (de)serializing Stack<T> is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            // Test is currently disabled until issue #53393 is addressed.

            //string json = "[1]";
            //object obj = JsonSerializer.Deserialize(json, typeof(Stack<int>));
            //if (!(TestHelper.AssertCollectionAndSerialize<Stack<int>>(obj, json)))
            //{
            //    return -1;
            //}

            return 100;
        }
    }
}
