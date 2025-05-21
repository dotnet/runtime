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
        
        [Flags]
        public enum ComplexFlagsEnum
        {
            None = 0,
            Flag1 = 1,
            Flag2 = 2,
            Flag4 = 4,
            Flag8 = 8,
            Flag16 = 16,
            Flag32 = 32,
            Combo1And2 = Flag1 | Flag2,
            Combo4And8And16 = Flag4 | Flag8 | Flag16,
            // No definition for Flag32 combinations
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

            var e2 = MyEnum2.BITS01 | MyEnum2.BIT3;
            string json2 = JsonSerializer.Serialize(e2, options);
            // Should be: "BITS01, BIT3" (as in .NET8)

            // Test that we get the combined fields rather than individual bits
            Assert.Contains("BITS01", json1);
            Assert.Contains("BIT3", json1);
            Assert.Contains("BITS01", json2);
            Assert.Contains("BIT3", json2);
            
            // Ensure we don't get numeric values
            Assert.DoesNotContain("11", json2);
        }
        
        [Fact]
        public void ComplexFlagsEnumTest()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter() }
            };
            
            // Test a complex combination including a flag that doesn't have
            // a direct field definition but can be represented by other fields
            var value = ComplexFlagsEnum.Combo1And2 | ComplexFlagsEnum.Combo4And8And16 | ComplexFlagsEnum.Flag32;
            string json = JsonSerializer.Serialize(value, options);
            
            // Verify we get string names and not numbers
            Assert.Contains("Combo1And2", json);
            Assert.Contains("Combo4And8And16", json);
            Assert.Contains("Flag32", json);
            Assert.DoesNotContain("63", json); // Numeric value of the combination
        }
    }
}