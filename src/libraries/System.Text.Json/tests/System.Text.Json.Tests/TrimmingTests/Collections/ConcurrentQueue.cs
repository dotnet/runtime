// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's warm up routine for (de)serializing ConcurrentQueue<T> is trimming-safe.
    /// </summary>
    internal class Program
    {
        // NOTE: ConcurrentQueue is only trimming safe because it's used by runtime thread pool. Except on single-threaded runtimes, where public parameterless constructor is trimmed.
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ConcurrentQueue<int>))]
        static int Main(string[] args)
        {
            string json = "[1]";
            object obj = JsonSerializer.Deserialize(json, typeof(ConcurrentQueue<int>));
            if (!(TestHelper.AssertCollectionAndSerialize<ConcurrentQueue<int>>(obj, json)))
            {
                return -1;
            }

            return 100;
        }
    }
}
