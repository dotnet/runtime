// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            if (json != """{"Day":0}""")
            {
                return -1;
            }

            json = JsonSerializer.Serialize(new ClassWithDaySourceGen(), Context.Default.ClassWithDaySourceGen);
            if (json != """{"Day":"Sun"}""")
            {
                return -2;
            }

            return 100;
        }
    }

    internal class ClassWithDay
    {
        public DayOfWeek Day { get; set; }
    }

    internal class ClassWithDaySourceGen
    {
        [JsonConverter(typeof(JsonStringEnumConverter<DayOfWeek>))]
        public DayOfWeek Day { get; set; }
    }

    internal enum DayOfWeek
    {
        [JsonStringEnumMemberName("Sun")]
        Sunday,
        [JsonStringEnumMemberName("Mon")]
        Monday,
        [JsonStringEnumMemberName("Tue")]
        Tuesday,
        [JsonStringEnumMemberName("Wed")]
        Wednesday,
        [JsonStringEnumMemberName("Thu")]
        Thursday,
        [JsonStringEnumMemberName("Fri")]
        Friday,
        [JsonStringEnumMemberName("Sat")]
        Saturday
    }

    [JsonSerializable(typeof(ClassWithDaySourceGen))]
    internal partial class Context : JsonSerializerContext;
}
