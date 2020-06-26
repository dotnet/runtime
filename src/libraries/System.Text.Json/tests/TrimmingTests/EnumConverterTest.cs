// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests the serializer (de)serializes enums appropriately,
    /// and that the enum converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = JsonSerializer.Serialize(new ClassWithDay());
            return json == @"{""Day"":5}" ? 100 : -1;
        }
    }

    internal class ClassWithDay
    {
        public DayOfWeek Day { get; set; } = DayOfWeek.Friday;
    }
}
