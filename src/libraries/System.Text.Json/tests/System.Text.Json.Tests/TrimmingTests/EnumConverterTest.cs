// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the enum converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = JsonSerializer.Serialize(new ClassWithDay());
            return json == @"{""Day"":0}" ? 100 : -1;
        }
    }

    internal class ClassWithDay
    {
        public DayOfWeek Day { get; set; }
    }
}
