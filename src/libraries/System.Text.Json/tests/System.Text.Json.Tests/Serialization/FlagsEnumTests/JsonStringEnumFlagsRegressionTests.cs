// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.Tests.Serialization.FlagsEnumTests
{
    public class JsonStringEnumFlagsRegressionTests
    {
        [Flags]
        public enum MyEnum1
        {
            UNKNOWN = 0,
            BIT0 = 1,
            BIT1 = 2,
            BIT2 = 4,
            BIT3 = 8,
            BITS01 = 3,
        }

        [Flags]
        public enum MyEnum2
        {
            UNKNOWN = 0,
            BIT0 = 1,
            // direct option for bit 1 missing
            BIT2 = 4,
            BIT3 = 8,
            BITS01 = 3,
        }

        [Fact]
        public void FlagsEnumTest()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter() }
            };

            var e1 = MyEnum1.BITS01 | MyEnum1.BIT3;
            string json1 = JsonSerializer.Serialize(e1, options);
            // Should be: "BITS01, BIT3" (as in .NET8)
            // Current behavior in .NET9: "BIT0, BIT1, BIT3"

            var e2 = MyEnum2.BITS01 | MyEnum2.BIT3;
            string json2 = JsonSerializer.Serialize(e2, options);
            // Should be: "BITS01, BIT3" (as in .NET8)
            // Current behavior in .NET9: "11" (numeric value)

            Assert.Contains("BITS01", json1);
            Assert.Contains("BIT3", json1);
            Assert.Contains("BITS01", json2);
            Assert.Contains("BIT3", json2);
        }
    }
}