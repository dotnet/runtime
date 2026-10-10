// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that source generated metadata for (de)serializing Hashtable is trimming-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            if (!TestHelper.RoundtripCollection("""{"Key":1}""", typeof(Hashtable), Context.Default))
            {
                return -1;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(Hashtable))]
    [JsonSerializable(typeof(JsonElement))]
    internal partial class Context : JsonSerializerContext;
}
