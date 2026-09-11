// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that source generated metadata for (de)serializing ConcurrentDictionary<TKey, TValue> is trimming safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            return TestHelper.RoundtripCollection("""{"Key":1}""", Context.Default.ConcurrentDictionaryStringInt32) ? 100 : -1;
        }
    }

    [JsonSerializable(typeof(ConcurrentDictionary<string, int>))]
    internal partial class Context : JsonSerializerContext;
}
