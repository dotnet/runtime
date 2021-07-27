// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the public parameterless ctor of the ConverterType property on
    /// JsonConstructorAttribute is preserved when needed in a trimmed application.
    /// </summary>
    internal class Program
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ClassWithDay))]
        static int Main(string[] args)
        {
            string json = JsonSerializer.Serialize(new ClassWithDay());
            return json == @"{""Day"":""Sunday""}" ? 100 : -1;
        }
    }

    internal class ClassWithDay
    {
        [JsonConverterAttribute(typeof(JsonStringEnumConverter))]
        public DayOfWeek Day { get; set; }
    }
}
