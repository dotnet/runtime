// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's warm up routine for (de)serializing ConcurrentQueue<T> is trimming-safe.
    /// </summary>
    internal class Program
    {
        if(OperatingSystem.IsBrowser())
        {
            // all the other platforms are using ConcurrentQueue and its constructor in the threadpool and so it's not trimmed
            // we are protecting it here
            // all the other targets will trim this code path
            new ConcurrentQueue<int>();
        }

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
