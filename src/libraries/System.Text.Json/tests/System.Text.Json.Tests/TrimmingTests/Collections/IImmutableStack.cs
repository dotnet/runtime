// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's warm up routine for (de)serializing IImmutableStack<T> is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            object obj = JsonSerializer.Deserialize(json, typeof(IImmutableStack<int>));
            if (!(TestHelper.AssertCollectionAndSerialize<IImmutableStack<int>>(obj, json)))
            {
                return -1;
            }

            return 100;
        }
    }
}
