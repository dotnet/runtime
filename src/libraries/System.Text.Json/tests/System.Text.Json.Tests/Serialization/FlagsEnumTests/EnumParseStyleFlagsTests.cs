// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;
using static System.AttributeTargets;

namespace System.Text.Json.Tests.Serialization.FlagsEnumTests
{
    /// <summary>
    /// Tests for flags enum serialization that incorporate test cases from the System.Runtime Enum.Parse test suite.
    /// </summary>
    public class EnumParseStyleFlagsTests
    {
        [Flags]
        public enum SimpleEnum
        {
            Red = 0x1,
            Blue = 0x2,
            Green = 0x3,
            Green_a = 0x3,
            Green_b = 0x3,
            B = 0x4
        }

        [Flags]
        public enum SByteEnum : sbyte
        {
            Min = sbyte.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = sbyte.MaxValue
        }

        [Flags]
        public enum ByteEnum : byte
        {
            Min = byte.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = byte.MaxValue
        }

        [Flags]
        public enum Int16Enum : short
        {
            Min = short.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = short.MaxValue
        }

        [Flags]
        public enum UInt16Enum : ushort
        {
            Min = ushort.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = ushort.MaxValue
        }

        [Flags]
        public enum Int32Enum
        {
            Min = int.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = int.MaxValue
        }

        [Flags]
        public enum UInt32Enum : uint
        {
            Min = uint.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = uint.MaxValue
        }

        [Flags]
        public enum Int64Enum : long
        {
            Min = long.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = long.MaxValue
        }

        [Flags]
        public enum UInt64Enum : ulong
        {
            Min = ulong.MinValue,
            One = 0x1,
            Two = 0x2,
            Max = ulong.MaxValue
        }

        [Flags]
        public enum FlagsSByteEnumWithNegativeValues : sbyte
        {
            A = 0x01,
            B = 0x02,
            C = 0x04,
            D = 0x08,
            E = 0x10,
            F = 0x20,
            G = 0x40,
            H = -0x80,
            I = -1
        }

        [Flags]
        public enum FlagsInt32EnumWithOverlappingNegativeValues
        {
            A = 0,
            B = -2,
            C = -3
        }
        // Create a JsonSerializerOptions instance with JsonStringEnumConverter
        private static readonly JsonSerializerOptions s_enumStringOptions = new()
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        [Fact]
        public void SimpleEnum_FlagCombinations()
        {
            // Test cases inspired by Parse_TestData in EnumTests.cs
            Assert.Equal("\"Red, Blue\"", JsonSerializer.Serialize(SimpleEnum.Red | SimpleEnum.Blue, s_enumStringOptions));
            Assert.Equal("\"Blue, Red, Green\"", JsonSerializer.Serialize(SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green, s_enumStringOptions));

            // Multiple values with same key (Green = Green_a = Green_b = 3)
            // The specific name used is an implementation detail, but the value should be serialized as a string
            string greenResult = JsonSerializer.Serialize(SimpleEnum.Green, s_enumStringOptions);
            Assert.Contains("\"Green", greenResult); // Could be Green, Green_a or Green_b
            
            // Test with leading and trailing spaces in enum names - should not be affected by JsonStringEnumConverter
            Assert.Equal("\"Red, B\"", JsonSerializer.Serialize(SimpleEnum.Red | SimpleEnum.B, s_enumStringOptions));
        }

        [Fact]
        public void ByteSizedEnums_FlagCombinations()
        {
            // SByte enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(SByteEnum.One | SByteEnum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            SByteEnum custom = (SByteEnum)5; // 5 = One (1) | custom value (4), but no direct SByteEnum.5
            Assert.Equal("\"5\"", JsonSerializer.Serialize(custom, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(SByteEnum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(SByteEnum.Max, s_enumStringOptions));

            // Byte enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(ByteEnum.One | ByteEnum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            ByteEnum customByte = (ByteEnum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(customByte, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(ByteEnum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(ByteEnum.Max, s_enumStringOptions));
        }

        [Fact]
        public void Int16SizedEnums_FlagCombinations()
        {
            // Int16 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(Int16Enum.One | Int16Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            Int16Enum custom = (Int16Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(custom, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(Int16Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(Int16Enum.Max, s_enumStringOptions));

            // UInt16 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(UInt16Enum.One | UInt16Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            UInt16Enum customUInt16 = (UInt16Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(customUInt16, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(UInt16Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(UInt16Enum.Max, s_enumStringOptions));
        }

        [Fact]
        public void Int32SizedEnums_FlagCombinations()
        {
            // Int32 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(Int32Enum.One | Int32Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            Int32Enum custom = (Int32Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(custom, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(Int32Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(Int32Enum.Max, s_enumStringOptions));

            // UInt32 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(UInt32Enum.One | UInt32Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            UInt32Enum customUInt32 = (UInt32Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(customUInt32, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(UInt32Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(UInt32Enum.Max, s_enumStringOptions));
        }

        [Fact]
        public void Int64SizedEnums_FlagCombinations()
        {
            // Int64 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(Int64Enum.One | Int64Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            Int64Enum custom = (Int64Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(custom, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(Int64Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(Int64Enum.Max, s_enumStringOptions));

            // UInt64 enum
            Assert.Equal("\"One, Two\"", JsonSerializer.Serialize(UInt64Enum.One | UInt64Enum.Two, s_enumStringOptions));
            
            // Value without direct enum member
            UInt64Enum customUInt64 = (UInt64Enum)5;
            Assert.Equal("\"5\"", JsonSerializer.Serialize(customUInt64, s_enumStringOptions));
            
            // Test min/max values
            Assert.Equal("\"Min\"", JsonSerializer.Serialize(UInt64Enum.Min, s_enumStringOptions));
            Assert.Equal("\"Max\"", JsonSerializer.Serialize(UInt64Enum.Max, s_enumStringOptions));
        }

        [Fact]
        public void FlagsEnumsWithNegativeValues()
        {
            // Test flags enums with negative values
            Assert.Equal("\"A\"", JsonSerializer.Serialize(FlagsSByteEnumWithNegativeValues.A, s_enumStringOptions));
            Assert.Equal("\"C\"", JsonSerializer.Serialize(FlagsSByteEnumWithNegativeValues.C, s_enumStringOptions));
            Assert.Equal("\"I\"", JsonSerializer.Serialize(FlagsSByteEnumWithNegativeValues.I, s_enumStringOptions));
            
            // Test combinations
            Assert.Equal("\"C, D\"", JsonSerializer.Serialize(
                FlagsSByteEnumWithNegativeValues.C | FlagsSByteEnumWithNegativeValues.D, 
                s_enumStringOptions));
            
            // Test with overlapping and negative values
            // In EnumTests.cs, this test asserts the value should be "C, D" because A gets masked out
            Assert.Equal("\"C, D\"", JsonSerializer.Serialize(
                FlagsSByteEnumWithNegativeValues.A | FlagsSByteEnumWithNegativeValues.C | FlagsSByteEnumWithNegativeValues.D, 
                s_enumStringOptions));
        }

        [Fact]
        public void FlagsEnumsWithOverlappingNegativeValues()
        {
            // Test individual values
            Assert.Equal("\"A\"", JsonSerializer.Serialize(FlagsInt32EnumWithOverlappingNegativeValues.A, s_enumStringOptions));
            Assert.Equal("\"B\"", JsonSerializer.Serialize(FlagsInt32EnumWithOverlappingNegativeValues.B, s_enumStringOptions));
            Assert.Equal("\"C\"", JsonSerializer.Serialize(FlagsInt32EnumWithOverlappingNegativeValues.C, s_enumStringOptions));
            
            // Test combination
            Assert.Equal("\"B, A\"", JsonSerializer.Serialize(
                FlagsInt32EnumWithOverlappingNegativeValues.A | FlagsInt32EnumWithOverlappingNegativeValues.B, 
                s_enumStringOptions));
            
            // Test -1 value which should format as "B, C"
            Assert.Equal("\"B, C\"", JsonSerializer.Serialize(
                (FlagsInt32EnumWithOverlappingNegativeValues)(-1), 
                s_enumStringOptions));
        }

        [Fact]
        public void AttributeTargetsFlags()
        {
            // Test AttributeTargets which is a [Flags] enum in the BCL
            Assert.Equal("\"Class, Delegate\"", JsonSerializer.Serialize(
                AttributeTargets.Class | AttributeTargets.Delegate,
                s_enumStringOptions));
            
            // Test with more flags
            Assert.Equal("\"Class, Delegate, Interface\"", JsonSerializer.Serialize(
                AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface,
                s_enumStringOptions));
            
            // Test All value
            Assert.Equal("\"All\"", JsonSerializer.Serialize(AttributeTargets.All, s_enumStringOptions));
        }
    }
}