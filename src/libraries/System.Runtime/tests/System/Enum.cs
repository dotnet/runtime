// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Tests.Common;

using Xunit;

public static class EnumTests
{
    [Theory]
    [InlineData("Red", false, true, SimpleEnum.Red)]
    [InlineData(" Red", false, true, SimpleEnum.Red)]
    [InlineData(" red ", true, true, SimpleEnum.Red)]
    [InlineData(" Red , Blue ", false, true, (SimpleEnum)3)]

    [InlineData("1", false, true, SimpleEnum.Red)]
    [InlineData(" 1 ", false, true, SimpleEnum.Red)]
    [InlineData("2", false, true, SimpleEnum.Blue)]
    [InlineData("99", false, true, (SimpleEnum)99)]

    [InlineData(" red ", false, false, default(SimpleEnum))] // No actual expected result
    [InlineData("Purple", false, false, default(SimpleEnum))]
    public static void TestParse(string value, bool ignoreCase, bool expectedCanParse, Enum expectedParseResult)
    {
        bool b;
        SimpleEnum e;
        if (!ignoreCase)
        {
            b = Enum.TryParse(value, out e);
            Assert.Equal(expectedCanParse, b);
            Assert.Equal(expectedParseResult, e);
        }

        b = Enum.TryParse(value, ignoreCase, out e);
        Assert.Equal(expectedCanParse, b);
        Assert.Equal(expectedParseResult, e);
    }

    [Theory]
    [InlineData(99, null)]
    [InlineData(1, "Red")]
    [InlineData(SimpleEnum.Red, "Red")]
    public static void TestGetName(object value, string expected)
    {
        string s = Enum.GetName(typeof(SimpleEnum), value);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestGetNameMultipleMatches()
    {
        // In the case of multiple matches, GetName returns one of them (which one is an implementation detail.)
        string s = Enum.GetName(typeof(SimpleEnum), 3);
        Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");
    }

    [Fact]
    public static void TestGetNameInvalid()
    {
        Type t = typeof(SimpleEnum);
        Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetName(null, 1)); // Enum type is null
        Assert.Throws<ArgumentNullException>("value", () => Enum.GetName(t, null)); // Value is null

        Assert.Throws<ArgumentException>(null, () => Enum.GetName(typeof(object), 1)); // Enum type is not an enum
        Assert.Throws<ArgumentException>("value", () => Enum.GetName(t, "Red")); // Value is not the type of the enum's raw data
        Assert.Throws<ArgumentException>("value", () => Enum.GetName(t, (IntPtr)0)); // Value is out of range
    }

    [Theory]
    [InlineData(0xffffffffffffff80LU, "Min")]
    [InlineData(0xffffff80u, null)]
    [InlineData(unchecked((int)(0xffffff80u)), "Min")]
    [InlineData(true, "One")]
    [InlineData((char)1, "One")]
    [InlineData(SimpleEnum.Red, "One")] // API doesnt't care if you pass in a completely different enum
    public static void TestGetNameNonIntegralTypes(object value, string expected)
    {
        /*
            * Despite what MSDN says, GetName() does not require passing in the exact integral type.
            * 
            * For the purposes of comparison:
            * 
            *  - The enum member value are normalized as follows:
            *    - unsigned ints zero-extended to 64-bits
            *    - signed ints sign-extended to 64-bits
            *
            *  - The value passed in as an argument to GetNames() is normalized as follows:
            *    - unsigned ints zero-extended to 64-bits
            *    - signed ints sign-extended to 64-bits
            *
            *  Then comparison is done on all 64 bits.
            */
        string s = Enum.GetName(typeof(SByteEnum), value);
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData(typeof(SimpleEnum), "Red", true)] //string
    [InlineData(typeof(SimpleEnum), "Green", true)]
    [InlineData(typeof(SimpleEnum), "Blue", true)]
    [InlineData(typeof(SimpleEnum), " Blue", false)]
    [InlineData(typeof(SimpleEnum), "blue", false)]
    [InlineData(typeof(SimpleEnum), "", false)]
    [InlineData(typeof(SimpleEnum), SimpleEnum.Red, true)] //Enum
    [InlineData(typeof(SimpleEnum), (SimpleEnum)99, false)]
    [InlineData(typeof(SimpleEnum), 1, true)] // Integer
    [InlineData(typeof(SimpleEnum), 99, false)]
    [InlineData(typeof(Int32Enum), 0x1 | 0x02, false)] // "Combos" do not pass
    public static void TestIsDefined(Type t, object value, bool expected)
    {
        bool b = Enum.IsDefined(t, value);
        Assert.Equal(expected, b);
    }

    [Fact]
    public static void TestIsDefinedInvalid()
    {
        Type t = typeof(SimpleEnum);
        
        Assert.Throws<ArgumentNullException>("enumType", () => Enum.IsDefined(null, 1)); // Enum type is null
        Assert.Throws<ArgumentNullException>("value", () => Enum.IsDefined(t, null)); // Value is null

        Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, Int32Enum.One)); // Value is different enum type
                                                                                           
        // Value is not a valid type (MSDN claims this should throw InvalidOperationException)
        Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, true));
        Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, 'a'));

        // Non-integers throw InvalidOperationException prior to Win8P.
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, (IntPtr)0));
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5));
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5f));
    }
    
    [Fact]
    public static void TestHasFlagInvalid()
    {
        Int32Enum e = (Int32Enum)0x3f06;

        Assert.Throws<ArgumentNullException>("flag", () => e.HasFlag(null)); // Flag is null
        Assert.Throws<ArgumentException>(null, () => e.HasFlag((SimpleEnum)0x2)); // Different enum type
    }

    [Theory]
    [InlineData((Int32Enum)0x3000, true)]
    [InlineData((Int32Enum)0x1000, true)]
    [InlineData((Int32Enum)0x0000, true)]
    [InlineData((Int32Enum)0x3f06, true)]
    [InlineData((Int32Enum)0x0010, false)]
    [InlineData((Int32Enum)0x3f16, false)]    
    public static void TestHasFlag(Enum flag, bool expected)
    {
        Int32Enum e = (Int32Enum)0x3f06;

        bool b = e.HasFlag(flag);
        Assert.Equal(expected, b);
    }

    [Fact]
    public static void TestToObject()
    {
        TestToObjectVerifySuccess<SByteEnum, sbyte>(42);
        TestToObjectVerifySuccess<SByteEnum, SByteEnum>((SByteEnum)0x42);
        TestToObjectVerifySuccess<UInt64Enum, ulong>(0x0123456789abcdefL);

        ulong l = 0x0ccccccccccccc2aL;
        ByteEnum e = (ByteEnum)(Enum.ToObject(typeof(ByteEnum), l));
        Assert.True((sbyte)e == 0x2a);
    }

    private static void TestToObjectVerifySuccess<E, T>(T value)
    {
        object oValue = value;
        object e = Enum.ToObject(typeof(E), oValue);
        Assert.Equal(e.GetType(), typeof(E));
        E expected = (E)(object)(value);
        object oExpected = (object)expected; // Workaround for Bartok codegen bug: Calling Object methods on enum through type variable fails (due to missing box)
        Assert.True(oExpected.Equals(e));
    }

    [Fact]
    public static void TestToObjectInvalid()
    {
        Assert.Throws<ArgumentNullException>("enumType", () => Enum.ToObject(null, 3)); // Enum type is null
        Assert.Throws<ArgumentNullException>("value", () => Enum.ToObject(typeof(SimpleEnum), null)); // Value is null

        Assert.Throws<ArgumentException>("enumType", () => Enum.ToObject(typeof(Enum), 1)); // Enum type is simply an enum
        Assert.Throws<ArgumentException>("value", () => Enum.ToObject(typeof(SimpleEnum), "Hello")); //Value is not a supported enum type
    }

    [Fact]
    public static void TestHashCode()
    {
        SimpleEnum e = (SimpleEnum)42;
        int h = e.GetHashCode();
        int h2 = e.GetHashCode();
        Assert.Equal(h, h2);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData((long)42, false)]
    [InlineData((Int32Enum)42, false)]
    [InlineData((Int64Enum)43, false)]
    [InlineData((Int64Enum)0x700000000000002aL, false)]
    [InlineData((Int64Enum)42, true)]
    public static void TestEquals(object obj, bool expected)
    {
        Int64Enum e = (Int64Enum)42;
        bool b = e.Equals(obj);
        Assert.Equal(expected, b);
    }

    [Theory]
    [InlineData(null, 1)] // Special case: All values are "greater than" null
    [InlineData(SimpleEnum.Red, 0)]
    [InlineData((SimpleEnum)0, 1)]
    [InlineData((SimpleEnum)2, -1)]
    public static void TestCompareTo(object target, int expected)
    {
        SimpleEnum e = SimpleEnum.Red;
        int i = CompareHelper.NormalizeCompare(e.CompareTo(target));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToInvalid()
    {
        Assert.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo((sbyte)1)); //Target is an enum type
        Assert.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo(Int32Enum.One)); //Target is a different enum type
    }

    [Theory]
    [InlineData(typeof(SByteEnum), typeof(sbyte))]
    [InlineData(typeof(ByteEnum), typeof(byte))]
    [InlineData(typeof(Int16Enum), typeof(short))]
    [InlineData(typeof(UInt16Enum), typeof(ushort))]
    [InlineData(typeof(Int32Enum), typeof(int))]
    [InlineData(typeof(UInt32Enum), typeof(uint))]
    [InlineData(typeof(Int64Enum), typeof(long))]
    [InlineData(typeof(UInt64Enum), typeof(ulong))]
    public static void TestGetUnderlyingType(Type enumType, Type expected)
    {
        Assert.Equal(expected, Enum.GetUnderlyingType(enumType));
    }

    [Fact]
    public static void TestGetUnderlyingTypeInvalid()
    {
        Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetUnderlyingType(null)); // Enum type is null
        Assert.Throws<ArgumentException>("enumType", () => Enum.GetUnderlyingType(typeof(Enum))); // Enum type is simply an enum
    }

    [Theory]
    [InlineData(typeof(SimpleEnum), 
        new[] { "Red", "Blue", "Green", "Green_a", "Green_b" },
        new object[] { SimpleEnum.Red, SimpleEnum.Blue, SimpleEnum.Green, SimpleEnum.Green_a, SimpleEnum.Green_b })]

    [InlineData(typeof(ByteEnum),
        new[] { "Min", "One", "Two", "Max" },
        new object[] { ByteEnum.Min, ByteEnum.One, ByteEnum.Two, ByteEnum.Max })]

    [InlineData(typeof(SByteEnum),
        new[] { "One", "Two", "Max", "Min" },
        new object[] { SByteEnum.One, SByteEnum.Two, SByteEnum.Max, SByteEnum.Min })]

    [InlineData(typeof(UInt16Enum),
        new[] { "Min", "One", "Two", "Max" },
        new object[] { UInt16Enum.Min, UInt16Enum.One, UInt16Enum.Two, UInt16Enum.Max })]

    [InlineData(typeof(Int16Enum),
        new[] { "One", "Two", "Max", "Min" },
        new object[] { Int16Enum.One, Int16Enum.Two, Int16Enum.Max, Int16Enum.Min })]

    [InlineData(typeof(UInt32Enum),
        new[] { "Min", "One", "Two", "Max" },
        new object[] { UInt32Enum.Min, UInt32Enum.One, UInt32Enum.Two, UInt32Enum.Max })]

    [InlineData(typeof(Int32Enum),
        new[] { "One", "Two", "Max", "Min" },
        new object[] { Int32Enum.One, Int32Enum.Two, Int32Enum.Max, Int32Enum.Min })]

    [InlineData(typeof(UInt64Enum),
        new[] { "Min", "One", "Two", "Max" },
        new object[] { UInt64Enum.Min, UInt64Enum.One, UInt64Enum.Two, UInt64Enum.Max })]

    [InlineData(typeof(Int64Enum),
        new[] { "One", "Two", "Max", "Min" },
        new object[] { Int64Enum.One, Int64Enum.Two, Int64Enum.Max, Int64Enum.Min })]

    public static void TestGetNamesAndValues(Type t, string[] expectedNames, object[] expectedValues)
    {
        string[] names = Enum.GetNames(t);
        Array values = Enum.GetValues(t);
        Assert.Equal(names.Length, values.Length);

        for (int i = 0; i < names.Length; i++)
        {
            Assert.Equal(expectedNames[i], names[i]);
            Assert.Equal(expectedValues[i], values.GetValue(i));
        }
    }

    [Theory]
    // Format: "D"
    [InlineData(ByteEnum.Min, "D", "0")]
    [InlineData(ByteEnum.One, "D", "1")]
    [InlineData(ByteEnum.Two, "D", "2")]
    [InlineData((ByteEnum)99, "D", "99")]
    [InlineData(ByteEnum.Max, "D", "255")]

    [InlineData(SByteEnum.Min, "D", "-128")]
    [InlineData(SByteEnum.One, "D", "1")]
    [InlineData(SByteEnum.Two, "D", "2")]
    [InlineData((SByteEnum)99, "D", "99")]
    [InlineData(SByteEnum.Max, "D", "127")]

    [InlineData(UInt16Enum.Min, "D", "0")]
    [InlineData(UInt16Enum.One, "D", "1")]
    [InlineData(UInt16Enum.Two, "D", "2")]
    [InlineData((UInt16Enum)99, "D", "99")]
    [InlineData(UInt16Enum.Max, "D", "65535")]

    [InlineData(Int16Enum.Min, "D", "-32768")]
    [InlineData(Int16Enum.One, "D", "1")]
    [InlineData(Int16Enum.Two, "D", "2")]
    [InlineData((Int16Enum)99, "D", "99")]
    [InlineData(Int16Enum.Max, "D", "32767")]

    [InlineData(UInt32Enum.Min, "D", "0")]
    [InlineData(UInt32Enum.One, "D", "1")]
    [InlineData(UInt32Enum.Two, "D", "2")]
    [InlineData((UInt32Enum)99, "D", "99")]
    [InlineData(UInt32Enum.Max, "D", "4294967295")]

    [InlineData(Int32Enum.Min, "D", "-2147483648")]
    [InlineData(Int32Enum.One, "D", "1")]
    [InlineData(Int32Enum.Two, "D", "2")]
    [InlineData((Int32Enum)99, "D", "99")]
    [InlineData(Int32Enum.Max, "D", "2147483647")]

    [InlineData(UInt64Enum.Min, "D", "0")]
    [InlineData(UInt64Enum.One, "D", "1")]
    [InlineData(UInt64Enum.Two, "D", "2")]
    [InlineData((UInt64Enum)99, "D", "99")]
    [InlineData(UInt64Enum.Max, "D", "18446744073709551615")]

    [InlineData(Int64Enum.Min, "D", "-9223372036854775808")]
    [InlineData(Int64Enum.One, "D", "1")]
    [InlineData(Int64Enum.Two, "D", "2")]
    [InlineData((Int64Enum)99, "D", "99")]
    [InlineData(Int64Enum.Max, "D", "9223372036854775807")]
    //Format "X": value in hex form without a leading "0x"
    [InlineData(ByteEnum.Min, "X", "00")]
    [InlineData(ByteEnum.One, "X", "01")]
    [InlineData(ByteEnum.Two, "X", "02")]
    [InlineData((ByteEnum)99, "X", "63")]
    [InlineData(ByteEnum.Max, "X", "FF")]

    [InlineData(SByteEnum.Min, "X", "80")]
    [InlineData(SByteEnum.One, "X", "01")]
    [InlineData(SByteEnum.Two, "X", "02")]
    [InlineData((SByteEnum)99, "X", "63")]
    [InlineData(SByteEnum.Max, "X", "7F")]

    [InlineData(UInt16Enum.Min, "X", "0000")]
    [InlineData(UInt16Enum.One, "X", "0001")]
    [InlineData(UInt16Enum.Two, "X", "0002")]
    [InlineData((UInt16Enum)99, "X", "0063")]
    [InlineData(UInt16Enum.Max, "X", "FFFF")]

    [InlineData(Int16Enum.Min, "X", "8000")]
    [InlineData(Int16Enum.One, "X", "0001")]
    [InlineData(Int16Enum.Two, "X", "0002")]
    [InlineData((Int16Enum)99, "X", "0063")]
    [InlineData(Int16Enum.Max, "X", "7FFF")]

    [InlineData(UInt32Enum.Min, "X", "00000000")]
    [InlineData(UInt32Enum.One, "X", "00000001")]
    [InlineData(UInt32Enum.Two, "X", "00000002")]
    [InlineData((UInt32Enum)99, "X", "00000063")]
    [InlineData(UInt32Enum.Max, "X", "FFFFFFFF")]

    [InlineData(Int32Enum.Min, "X", "80000000")]
    [InlineData(Int32Enum.One, "X", "00000001")]
    [InlineData(Int32Enum.Two, "X", "00000002")]
    [InlineData((Int32Enum)99, "X", "00000063")]
    [InlineData(Int32Enum.Max, "X", "7FFFFFFF")]

    [InlineData(UInt64Enum.Min, "X", "0000000000000000")]
    [InlineData(UInt64Enum.One, "X", "0000000000000001")]
    [InlineData(UInt64Enum.Two, "X", "0000000000000002")]
    [InlineData((UInt64Enum)99, "X", "0000000000000063")]
    [InlineData(UInt64Enum.Max, "X", "FFFFFFFFFFFFFFFF")]

    [InlineData(Int64Enum.Min, "X", "8000000000000000")]
    [InlineData(Int64Enum.One, "X", "0000000000000001")]
    [InlineData(Int64Enum.Two, "X", "0000000000000002")]
    [InlineData((Int64Enum)99, "X", "0000000000000063")]
    [InlineData(Int64Enum.Max, "X", "7FFFFFFFFFFFFFFF")]

    // Format "F". value is treated as a bit field that contains one or more flags that consist of one or more bits.
    // If value is equal to a combination of named enumerated constants, a delimiter-separated list of the names 
    // of those constants is returned. value is searched for flags, going from the flag with the largest value 
    // to the smallest value. For each flag that corresponds to a bit field in value, the name of the constant 
    // is concatenated to the delimiter-separated list. The value of that flag is then excluded from further 
    // consideration, and the search continues for the next flag.
    //
    // If value is not equal to a combination of named enumerated constants, the decimal equivalent of value is returned. 
    [InlineData(SimpleEnum.Red, "F", "Red")]
    [InlineData(SimpleEnum.Blue, "F", "Blue")]
    [InlineData((SimpleEnum)99, "F", "99")]
    [InlineData((SimpleEnum)0, "F", "0")] // Not found

    [InlineData((ByteEnum)0, "F", "Min")]
    [InlineData((ByteEnum)3, "F", "One, Two")]
    [InlineData((ByteEnum)0xff, "F", "Max")] // Larger values take precedence (and remove the bits from consideration.)
                                             
    // Format "G": If value is equal to a named enumerated constant, the name of that constant is returned.
    // Otherwise, if "[Flags]" present, do as Format "F" - else return the decimal value of "value".
    [InlineData(SimpleEnum.Red, "G", "Red")]
    [InlineData(SimpleEnum.Blue, "G", "Blue")]
    [InlineData((SimpleEnum)99, "G", "99")]
    [InlineData((SimpleEnum)0, "G", "0")] // Not found

    [InlineData((ByteEnum)0, "G", "Min")]
    [InlineData((ByteEnum)3, "G", "3")] // No [Flags] attribute
    [InlineData((ByteEnum)0xff, "F", "Max")] // Larger values take precedence (and remove the bits from consideration.)
    [InlineData(AttributeTargets.Class | AttributeTargets.Delegate, "F", "Class, Delegate")] // [Flags] attribute
    public static void TestFormat(Enum e, string format, string expected)
    {
        string s = e.ToString(format);
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestFormatMultipleMatches()
    {
        string s = ((SimpleEnum)3).ToString("F");
        Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");

        s = ((SimpleEnum)3).ToString("G");
        Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");
    }

    [Theory]
    [InlineData(SimpleEnum.Red, "Red")]
    [InlineData(1, "Red")] // Underlying integral
    public static void TestFormat(object value, string expected)
    {
        string s = Enum.Format(typeof(SimpleEnum), value, "F");
        Assert.Equal(expected, s);
    }

    [Fact]
    public static void TestFormatInvalid()
    {
        Assert.Throws<ArgumentNullException>("enumType", () => Enum.Format(null, (Int32Enum)1, "F")); // Enum type is null
        Assert.Throws<ArgumentNullException>("value", () => Enum.Format(typeof(SimpleEnum), null, "F")); // Value is null
        
        Assert.Throws<ArgumentException>("enumType", () => Enum.Format(typeof(object), 1, "F")); // Enum type is not an enum type
        
        Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (Int32Enum)1, "F")); // Value is of the wrong enum type
        
        Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (short)1, "F")); // Value is of the wrong integral
        Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), "Red", "F")); // Value is of the wrong integral
    }

    private enum SimpleEnum
    {
        Red = 1,
        Blue = 2,
        Green = 3,
        Green_a = 3,
        Green_b = 3
    }

    private enum ByteEnum : byte
    {
        Min = byte.MinValue,
        One = 1,
        Two = 2,
        Max = byte.MaxValue,
    }

    private enum SByteEnum : sbyte
    {
        Min = sbyte.MinValue,
        One = 1,
        Two = 2,
        Max = sbyte.MaxValue,
    }

    private enum UInt16Enum : ushort
    {
        Min = ushort.MinValue,
        One = 1,
        Two = 2,
        Max = ushort.MaxValue,
    }

    private enum Int16Enum : short
    {
        Min = short.MinValue,
        One = 1,
        Two = 2,
        Max = short.MaxValue,
    }

    private enum UInt32Enum : uint
    {
        Min = uint.MinValue,
        One = 1,
        Two = 2,
        Max = uint.MaxValue,
    }

    private enum Int32Enum : int
    {
        Min = int.MinValue,
        One = 1,
        Two = 2,
        Max = int.MaxValue,
    }

    private enum UInt64Enum : ulong
    {
        Min = ulong.MinValue,
        One = 1,
        Two = 2,
        Max = ulong.MaxValue,
    }

    private enum Int64Enum : long
    {
        Min = long.MinValue,
        One = 1,
        Two = 2,
        Max = long.MaxValue,
    }
}
