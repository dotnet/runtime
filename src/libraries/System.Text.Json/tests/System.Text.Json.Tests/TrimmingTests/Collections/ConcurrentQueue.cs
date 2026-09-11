// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that source generated metadata for (de)serializing ConcurrentQueue<T> is trimming safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            return TestHelper.RoundtripCollection("[1]", Context.Default.ConcurrentQueueInt32) ? 100 : -1;
        }
    }

    [JsonSerializable(typeof(ConcurrentQueue<int>))]
    internal partial class Context : JsonSerializerContext;
}
