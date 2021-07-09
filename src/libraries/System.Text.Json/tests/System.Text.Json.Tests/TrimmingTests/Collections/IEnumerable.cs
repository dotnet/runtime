// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's warm up routine for (de)serializing IEnumerable is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            object obj = JsonSerializer.Deserialize(json, typeof(IEnumerable));
            if (!(TestHelper.AssertCollectionAndSerialize<IEnumerable>(obj, json)))
            {
                return -1;
            }

            return 100;
        }
    }
}
