// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that source generated metadata for (de)serializing IEnumerable<T> is trimming-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            if (!TestHelper.RoundtripCollection("[1]", typeof(IEnumerable<int>), Context.Default))
            {
                return -1;
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(IEnumerable<int>))]
    internal partial class Context : JsonSerializerContext;
}
