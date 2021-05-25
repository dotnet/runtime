// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the enum converter factory works with trimming.
    /// </summary>
    internal class Program
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ClassWithDay))]
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
