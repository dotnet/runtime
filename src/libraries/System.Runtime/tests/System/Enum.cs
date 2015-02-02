// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static unsafe class EnumTests
{
    [Fact]
    public static void TestParse()
    {
        {
            Type t = typeof(SimpleEnum);
            bool b;
            SimpleEnum e;

            b = Enum.TryParse<SimpleEnum>("Red", out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Red);

            b = Enum.TryParse<SimpleEnum>(" Red ", out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Red);

            b = Enum.TryParse<SimpleEnum>(" red ", out e);
            Assert.True(!b);

            b = Enum.TryParse<SimpleEnum>(" red ", false, out e);
            Assert.True(!b);

            b = Enum.TryParse<SimpleEnum>(" red ", true, out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Red);

            b = Enum.TryParse<SimpleEnum>(" Red , Blue ", out e);
            Assert.True(b);
            Assert.Equal(e, (SimpleEnum)3);

            b = Enum.TryParse<SimpleEnum>("Purple", out e);
            Assert.True(!b);

            b = Enum.TryParse<SimpleEnum>("1", out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Red);

            b = Enum.TryParse<SimpleEnum>(" 1 ", out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Red);

            b = Enum.TryParse<SimpleEnum>("2", out e);
            Assert.True(b);
            Assert.Equal(e, SimpleEnum.Blue);

            b = Enum.TryParse<SimpleEnum>("99", out e);
            Assert.True(b);
            Assert.Equal(e, (SimpleEnum)99);

            return;
        }
    }

    [Fact]
    public static void TestGetName()
    {
        String s;
        {
            Type t = typeof(SimpleEnum);
            s = Enum.GetName(t, 99);
            Assert.Equal(s, null);

            s = Enum.GetName(t, 1);
            Assert.Equal(s, "Red");

            s = Enum.GetName(t, SimpleEnum.Red);
            Assert.Equal(s, "Red");

            // In the case of multiple matches, GetName returns one of them (which one is an implementation detail.)
            s = Enum.GetName(t, 3);
            Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");
        }

        // Negative tests
        {
            Type t = typeof(SimpleEnum);
            Assert.Throws<ArgumentNullException>(() => Enum.GetName(null, 1));
            Assert.Throws<ArgumentNullException>(() => Enum.GetName(t, null));
            Assert.Throws<ArgumentNullException>(() => Enum.GetName(typeof(Object), null));
            Assert.Throws<ArgumentException>(() => Enum.GetName(t, "Red"));
            Assert.Throws<ArgumentException>(() => Enum.GetName(t, (IntPtr)0));
        }

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

            Type t = typeof(SByteEnum);
            s = Enum.GetName(t, 0xffffffffffffff80LU);
            Assert.Equal(s, "Min");

            s = Enum.GetName(t, 0xffffff80u);
            Assert.Equal(s, null);

            s = Enum.GetName(t, unchecked((int)(0xffffff80u)));
            Assert.Equal(s, "Min");

            s = Enum.GetName(t, true);
            Assert.Equal(s, "One");

            s = Enum.GetName(t, (char)1);
            Assert.Equal(s, "One");

            // The api doesn't even care if you pass in a completely different enum!
            s = Enum.GetName(t, SimpleEnum.Red);
            Assert.Equal(s, "One");
        }
        return;
    }

    [Fact]
    public static void TestIsDefined()
    {
        Type t = typeof(SimpleEnum);
        bool b;

        // Value can be a string...
        b = Enum.IsDefined(t, "Red");
        Assert.True(b);

        b = Enum.IsDefined(t, "Green");
        Assert.True(b);

        b = Enum.IsDefined(t, "Blue");
        Assert.True(b);

        b = Enum.IsDefined(t, " Blue");
        Assert.True(!b);

        b = Enum.IsDefined(t, "blue");
        Assert.True(!b);

        // or an Enum value
        b = Enum.IsDefined(t, SimpleEnum.Red);
        Assert.True(b);

        b = Enum.IsDefined(t, (SimpleEnum)(99));
        Assert.True(!b);

        // but not a different enum.
        Assert.Throws<ArgumentException>(() => Enum.IsDefined(t, Int32Enum.One));

        // or the underlying integer
        b = Enum.IsDefined(t, 1);
        Assert.True(b);

        b = Enum.IsDefined(t, 99);
        Assert.True(!b);

        // but not just any integer type.
        Assert.Throws<ArgumentException>(() => Enum.IsDefined(t, (short)1));

        Assert.Throws<ArgumentException>(() => Enum.IsDefined(t, (uint)1));

        // "Combos" do not pass.
        b = Enum.IsDefined(typeof(Int32Enum), 0x1 | 0x2);
        Assert.True(!b);

        // Other negative tests
        Assert.Throws<ArgumentNullException>(() => Enum.IsDefined(null, 1));
        Assert.Throws<ArgumentNullException>(() => Enum.IsDefined(t, null));

        // These throws ArgumentException (though MSDN claims it should throw InvalidOperationException)
        Assert.Throws<ArgumentException>(() => Enum.IsDefined(t, true));
        Assert.Throws<ArgumentException>(() => Enum.IsDefined(t, 'a'));

        // Non-integers throw InvalidOperationException prior to Win8P.
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, (IntPtr)0));
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5));
        Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5f));

        return;
    }

    [Fact]
    public static void TestHasFlag()
    {
        EI32 e = (EI32)0x3f06;

        try
        {
            e.HasFlag(null);
            Assert.True(false, "HasFlag should have thrown.");
        }
        catch (ArgumentNullException)
        {
        }

        try
        {
            e.HasFlag((EI32a)0x2);
            Assert.True(false, "HasFlag should have thrown.");
        }
        catch (ArgumentException)
        {
        }

        bool b;

        b = e.HasFlag((EI32)(0x3000));
        Assert.True(b);

        b = e.HasFlag((EI32)(0x1000));
        Assert.True(b);

        b = e.HasFlag((EI32)(0x0000));
        Assert.True(b);

        b = e.HasFlag((EI32)(0x0010));
        Assert.False(b);

        b = e.HasFlag((EI32)(0x3f06));
        Assert.True(b);

        b = e.HasFlag((EI32)(0x3f16));
        Assert.False(b);
    }

    [Fact]
    public static void TestToObject()
    {
        Object o;
        o = 3;

        try
        {
            Enum.ToObject(null, o);
            Assert.True(false, "ToObject() should have thrown.");
        }
        catch (ArgumentNullException)
        {
        }

        o = null;
        try
        {
            Enum.ToObject(typeof(EI8), o);
            Assert.True(false, "ToObject() should have thrown.");
        }
        catch (ArgumentNullException)
        {
        }

        o = 1;
        try
        {
            Enum.ToObject(typeof(Enum), o);
            Assert.True(false, "ToObject() should have thrown.");
        }
        catch (ArgumentException)
        {
        }

        try
        {
            o = "Hello";
            Enum.ToObject(typeof(EI8), o);
            Assert.True(false, "ToObject() should have thrown.");
        }
        catch (ArgumentException)
        {
        }

        TestToObjectVerifySuccess<EI8, sbyte>(42);
        TestToObjectVerifySuccess<EI8, EI8>((EI8)0x42);
        TestToObjectVerifySuccess<EU64, ulong>(0x0123456789abcdefL);

        ulong l = 0x0ccccccccccccc2aL;
        EI8 e = (EI8)(Enum.ToObject(typeof(EI8), l));
        Assert.True((sbyte)e == 0x2a);
    }

    private static void TestToObjectVerifySuccess<E, T>(T value)
    {
        Object oValue = value;
        Object e = Enum.ToObject(typeof(E), oValue);
        Assert.Equal(e.GetType(), typeof(E));
        E expected = (E)(Object)(value);
        Object oExpected = (Object)expected; // Workaround for Bartok codegen bug: Calling Object methods on enum through type variable fails (due to missing box)
        Assert.True(oExpected.Equals(e));
    }

    [Fact]
    public static void TestHashCode()
    {
        EI64 e = (EI64)42;
        int h = e.GetHashCode();
        int h2 = e.GetHashCode();
        Assert.Equal(h, h2);
    }

    [Fact]
    public static void TestEquals()
    {
        EI64 e = (EI64)42;
        bool b;

        b = e.Equals(null);
        Assert.False(b);

        b = e.Equals((long)42);
        Assert.False(b);

        b = e.Equals((EI32)42);
        Assert.False(b);

        b = e.Equals((EI64)43);
        Assert.False(b);

        long l = 0x700000000000002aL;
        b = e.Equals((EI64)l);
        Assert.False(b);

        b = e.Equals((EI64)42);
        Assert.True(b);
    }

    [Fact]
    public static void TestCompareTo()
    {
        EI8 e = EI8.One;
        int result;

        // Special case: All values are "greater than" null.
        Object other = null;
        result = e.CompareTo(other);
        Assert.Equal(result, 1);

        try
        {
            sbyte b = 1;
            result = e.CompareTo(b);
            Assert.True(false, "CompareTo should have failed.");
        }
        catch (ArgumentException)
        {
        }

        other = EI8.One;
        result = e.CompareTo(other);
        Assert.Equal(result, 0);
        other = (EI8)(0);
        result = e.CompareTo(other);
        Assert.True(result > 0);
        other = (EI8)(2);
        result = e.CompareTo(other);
        Assert.True(result < 0);
    }

    [Fact]
    public static void TestGetUnderlyingType()
    {
        Type t;

        try
        {
            Enum.GetUnderlyingType(null);
            Assert.True(false, "GetUnderlyingType should have thrown.");
        }
        catch (ArgumentNullException)
        {
        }

        try
        {
            Enum.GetUnderlyingType(typeof(Enum));
            Assert.True(false, "GetUnderlyingType should have thrown.");
        }
        catch (ArgumentException)
        {
        }

        t = Enum.GetUnderlyingType(typeof(EI8));
        Assert.Equal(t, typeof(SByte));

        t = Enum.GetUnderlyingType(typeof(EU8));
        Assert.Equal(t, typeof(Byte));

        t = Enum.GetUnderlyingType(typeof(EI16));
        Assert.Equal(t, typeof(Int16));

        t = Enum.GetUnderlyingType(typeof(EU16));
        Assert.Equal(t, typeof(UInt16));

        t = Enum.GetUnderlyingType(typeof(EI32));
        Assert.Equal(t, typeof(Int32));

        t = Enum.GetUnderlyingType(typeof(EU32));
        Assert.Equal(t, typeof(UInt32));

        t = Enum.GetUnderlyingType(typeof(EI64));
        Assert.Equal(t, typeof(Int64));

        t = Enum.GetUnderlyingType(typeof(EU64));
        Assert.Equal(t, typeof(UInt64));

    }

    private enum EI8 : sbyte
    {
        One = 1,
    }

    private enum EU8 : byte
    {
    }

    private enum EI16 : short
    {
    }

    private enum EU16 : ushort
    {
    }

    private enum EI32 : int
    {
    }

    private enum EI32a : int
    {
    }

    private enum EU32 : uint
    {
    }

    private enum EI64 : long
    {
    }

    private enum EU64 : ulong
    {
    }

    [Fact]
    public static void TestGetNamesAndValues()
    {
        {
            String[] names = Enum.GetNames(typeof(SimpleEnum));
            SimpleEnum[] values = (SimpleEnum[])(Enum.GetValues(typeof(SimpleEnum)));

            int i = 0;
            Assert.Equal(names[i], "Red");
            Assert.Equal(values[i], SimpleEnum.Red);
            i++;
            Assert.Equal(names[i], "Blue");
            Assert.Equal(values[i], SimpleEnum.Blue);
            i++;
            Assert.Equal(names[i], "Green");
            Assert.Equal(values[i], SimpleEnum.Green);
            i++;
            Assert.Equal(names[i], "Green_a");
            Assert.Equal(values[i], SimpleEnum.Green_a);
            i++;
            Assert.Equal(names[i], "Green_b");
            Assert.Equal(values[i], SimpleEnum.Green_a);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(ByteEnum));
            ByteEnum[] values = (ByteEnum[])(Enum.GetValues(typeof(ByteEnum)));

            int i = 0;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], ByteEnum.Min);
            i++;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], ByteEnum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], ByteEnum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], ByteEnum.Max);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(SByteEnum));
            SByteEnum[] values = (SByteEnum[])(Enum.GetValues(typeof(SByteEnum)));

            int i = 0;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], SByteEnum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], SByteEnum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], SByteEnum.Max);
            i++;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], SByteEnum.Min);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(UInt16Enum));
            UInt16Enum[] values = (UInt16Enum[])(Enum.GetValues(typeof(UInt16Enum)));

            int i = 0;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], UInt16Enum.Min);
            i++;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], UInt16Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], UInt16Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], UInt16Enum.Max);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(Int16Enum));
            Int16Enum[] values = (Int16Enum[])(Enum.GetValues(typeof(Int16Enum)));

            int i = 0;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], Int16Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], Int16Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], Int16Enum.Max);
            i++;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], Int16Enum.Min);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(UInt32Enum));
            UInt32Enum[] values = (UInt32Enum[])(Enum.GetValues(typeof(UInt32Enum)));

            int i = 0;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], UInt32Enum.Min);
            i++;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], UInt32Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], UInt32Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], UInt32Enum.Max);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(Int32Enum));
            Int32Enum[] values = (Int32Enum[])(Enum.GetValues(typeof(Int32Enum)));

            int i = 0;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], Int32Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], Int32Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], Int32Enum.Max);
            i++;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], Int32Enum.Min);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(UInt64Enum));
            UInt64Enum[] values = (UInt64Enum[])(Enum.GetValues(typeof(UInt64Enum)));

            int i = 0;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], UInt64Enum.Min);
            i++;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], UInt64Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], UInt64Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], UInt64Enum.Max);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }

        {
            String[] names = Enum.GetNames(typeof(Int64Enum));
            Int64Enum[] values = (Int64Enum[])(Enum.GetValues(typeof(Int64Enum)));

            int i = 0;
            Assert.Equal(names[i], "One");
            Assert.Equal(values[i], Int64Enum.One);
            i++;
            Assert.Equal(names[i], "Two");
            Assert.Equal(values[i], Int64Enum.Two);
            i++;
            Assert.Equal(names[i], "Max");
            Assert.Equal(values[i], Int64Enum.Max);
            i++;
            Assert.Equal(names[i], "Min");
            Assert.Equal(values[i], Int64Enum.Min);
            i++;

            Assert.Equal(names.Length, i);
            Assert.Equal(values.Length, i);
        }
    }

    [Fact]
    public static void TestFormatD()
    {
        String s;

        s = ByteEnum.Min.ToString("D");
        Assert.Equal(s, "0");

        s = ByteEnum.One.ToString("D");
        Assert.Equal(s, "1");

        s = ByteEnum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((ByteEnum)99).ToString("D");
        Assert.Equal(s, "99");

        s = ByteEnum.Max.ToString("D");
        Assert.Equal(s, "255");

        s = SByteEnum.Min.ToString("D");
        Assert.Equal(s, "-128");

        s = SByteEnum.One.ToString("D");
        Assert.Equal(s, "1");

        s = SByteEnum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((SByteEnum)99).ToString("D");
        Assert.Equal(s, "99");

        s = SByteEnum.Max.ToString("D");
        Assert.Equal(s, "127");

        s = UInt16Enum.Min.ToString("D");
        Assert.Equal(s, "0");

        s = UInt16Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = UInt16Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((UInt16Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = UInt16Enum.Max.ToString("D");
        Assert.Equal(s, "65535");

        s = Int16Enum.Min.ToString("D");
        Assert.Equal(s, "-32768");

        s = Int16Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = Int16Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((Int16Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = Int16Enum.Max.ToString("D");
        Assert.Equal(s, "32767");

        s = UInt32Enum.Min.ToString("D");
        Assert.Equal(s, "0");

        s = UInt32Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = UInt32Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((UInt32Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = UInt32Enum.Max.ToString("D");
        Assert.Equal(s, "4294967295");

        s = Int32Enum.Min.ToString("D");
        Assert.Equal(s, "-2147483648");

        s = Int32Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = Int32Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((Int32Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = Int32Enum.Max.ToString("D");
        Assert.Equal(s, "2147483647");

        s = UInt64Enum.Min.ToString("D");
        Assert.Equal(s, "0");

        s = UInt64Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = UInt64Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((UInt64Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = UInt64Enum.Max.ToString("D");
        Assert.Equal(s, "18446744073709551615");

        s = Int64Enum.Min.ToString("D");
        Assert.Equal(s, "-9223372036854775808");

        s = Int64Enum.One.ToString("D");
        Assert.Equal(s, "1");

        s = Int64Enum.Two.ToString("D");
        Assert.Equal(s, "2");

        s = ((Int64Enum)99).ToString("D");
        Assert.Equal(s, "99");

        s = Int64Enum.Max.ToString("D");
        Assert.Equal(s, "9223372036854775807");
    }

    [Fact]
    public static void TestFormatX()
    {
        // Format "X": Represents value in hex form without a leading "0x"
        String s;

        s = ByteEnum.Min.ToString("X");
        Assert.Equal(s, "00");

        s = ByteEnum.One.ToString("X");
        Assert.Equal(s, "01");

        s = ByteEnum.Two.ToString("X");
        Assert.Equal(s, "02");

        s = ((ByteEnum)99).ToString("X");
        Assert.Equal(s, "63");

        s = ByteEnum.Max.ToString("X");
        Assert.Equal(s, "FF");

        s = SByteEnum.Min.ToString("X");
        Assert.Equal(s, "80");

        s = SByteEnum.One.ToString("X");
        Assert.Equal(s, "01");

        s = SByteEnum.Two.ToString("X");
        Assert.Equal(s, "02");

        s = ((SByteEnum)99).ToString("X");
        Assert.Equal(s, "63");

        s = SByteEnum.Max.ToString("X");
        Assert.Equal(s, "7F");

        s = UInt16Enum.Min.ToString("X");
        Assert.Equal(s, "0000");

        s = UInt16Enum.One.ToString("X");
        Assert.Equal(s, "0001");

        s = UInt16Enum.Two.ToString("X");
        Assert.Equal(s, "0002");

        s = ((UInt16Enum)99).ToString("X");
        Assert.Equal(s, "0063");

        s = UInt16Enum.Max.ToString("X");
        Assert.Equal(s, "FFFF");

        s = Int16Enum.Min.ToString("X");
        Assert.Equal(s, "8000");

        s = Int16Enum.One.ToString("X");
        Assert.Equal(s, "0001");

        s = Int16Enum.Two.ToString("X");
        Assert.Equal(s, "0002");

        s = ((Int16Enum)99).ToString("X");
        Assert.Equal(s, "0063");

        s = Int16Enum.Max.ToString("X");
        Assert.Equal(s, "7FFF");

        s = UInt32Enum.Min.ToString("X");
        Assert.Equal(s, "00000000");

        s = UInt32Enum.One.ToString("X");
        Assert.Equal(s, "00000001");

        s = UInt32Enum.Two.ToString("X");
        Assert.Equal(s, "00000002");

        s = ((UInt32Enum)99).ToString("X");
        Assert.Equal(s, "00000063");

        s = UInt32Enum.Max.ToString("X");
        Assert.Equal(s, "FFFFFFFF");

        s = Int32Enum.Min.ToString("X");
        Assert.Equal(s, "80000000");

        s = Int32Enum.One.ToString("X");
        Assert.Equal(s, "00000001");

        s = Int32Enum.Two.ToString("X");
        Assert.Equal(s, "00000002");

        s = ((Int32Enum)99).ToString("X");
        Assert.Equal(s, "00000063");

        s = Int32Enum.Max.ToString("X");
        Assert.Equal(s, "7FFFFFFF");

        s = UInt64Enum.Min.ToString("X");
        Assert.Equal(s, "0000000000000000");

        s = UInt64Enum.One.ToString("X");
        Assert.Equal(s, "0000000000000001");

        s = UInt64Enum.Two.ToString("X");
        Assert.Equal(s, "0000000000000002");

        s = ((UInt64Enum)99).ToString("X");
        Assert.Equal(s, "0000000000000063");

        s = UInt64Enum.Max.ToString("X");
        Assert.Equal(s, "FFFFFFFFFFFFFFFF");

        s = Int64Enum.Min.ToString("X");
        Assert.Equal(s, "8000000000000000");

        s = Int64Enum.One.ToString("X");
        Assert.Equal(s, "0000000000000001");

        s = Int64Enum.Two.ToString("X");
        Assert.Equal(s, "0000000000000002");

        s = ((Int64Enum)99).ToString("X");
        Assert.Equal(s, "0000000000000063");

        s = Int64Enum.Max.ToString("X");
        Assert.Equal(s, "7FFFFFFFFFFFFFFF");
    }

    [Fact]
    public static void TestFormatF()
    {
        // Format "F". value is treated as a bit field that contains one or more flags that consist of one or more bits.
        // If value is equal to a combination of named enumerated constants, a delimiter-separated list of the names 
        // of those constants is returned. value is searched for flags, going from the flag with the largest value 
        // to the smallest value. For each flag that corresponds to a bit field in value, the name of the constant 
        // is concatenated to the delimiter-separated list. The value of that flag is then excluded from further 
        // consideration, and the search continues for the next flag.
        //
        //If value is not equal to a combination of named enumerated constants, the decimal equivalent of value is returned. 
        String s;

        s = SimpleEnum.Red.ToString("F");
        Assert.Equal(s, "Red");

        s = SimpleEnum.Blue.ToString("F");
        Assert.Equal(s, "Blue");

        s = ((SimpleEnum)3).ToString("F");
        Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");

        s = ((SimpleEnum)99).ToString("F");
        Assert.Equal(s, "99");

        s = ((SimpleEnum)0).ToString("F");
        Assert.Equal(s, "0");  // Not found.

        s = ((ByteEnum)0).ToString("F");
        Assert.Equal(s, "Min");  // Found

        s = ((ByteEnum)3).ToString("F");
        Assert.Equal(s, "One, Two");  // Found

        // Larger values take precedence (and remove the bits from consideration.)
        s = ((ByteEnum)0xff).ToString("F");
        Assert.Equal(s, "Max");  // Found
    }

    [Fact]
    public static void TestFormatG()
    {
        // Format "G": If value is equal to a named enumerated constant, the name of that constant is returned.
        // Otherwise, if "[Flags]" present, do as Format "F" - else return the decimal value of "value".
        String s;

        s = SimpleEnum.Red.ToString("G");
        Assert.Equal(s, "Red");

        s = SimpleEnum.Blue.ToString("G");
        Assert.Equal(s, "Blue");

        s = ((SimpleEnum)3).ToString("G");
        Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");

        s = ((SimpleEnum)99).ToString("G");
        Assert.Equal(s, "99");

        s = ((SimpleEnum)0).ToString("G");
        Assert.Equal(s, "0");  // Not found.

        s = ((ByteEnum)0).ToString("G");
        Assert.Equal(s, "Min");  // Found

        s = ((ByteEnum)3).ToString("G");
        Assert.Equal(s, "3");  // No [Flags] attribute

        // Larger values take precedence (and remove the bits from consideration.)
        s = ((ByteEnum)0xff).ToString("G");
        Assert.Equal(s, "Max");  // Found

        // An enum with [Flags]
        s = (AttributeTargets.Class | AttributeTargets.Delegate).ToString("G");
        Assert.Equal(s, "Class, Delegate");
    }

    [Fact]
    public static void TestFormat()
    {
        String s;

        s = Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "F");
        Assert.Equal(s, "Red");

        // Can pass enum or exact underlying integral.
        s = Enum.Format(typeof(SimpleEnum), 1, "F");
        Assert.Equal(s, "Red");

        Assert.Throws<ArgumentNullException>(() => Enum.Format(null, (Int32Enum)1, "F"));
        Assert.Throws<ArgumentNullException>(() => Enum.Format(typeof(SimpleEnum), null, "F"));

        // Not an enum type.
        Assert.Throws<ArgumentException>(() => Enum.Format(typeof(Object), 1, "F"));

        // Wrong enumType.
        Assert.Throws<ArgumentException>(() => Enum.Format(typeof(SimpleEnum), (Int32Enum)1, "F"));

        // Wrong integral.
        Assert.Throws<ArgumentException>(() => Enum.Format(typeof(SimpleEnum), (short)1, "F"));

        // Not an integral.
        Assert.Throws<ArgumentException>(() => Enum.Format(typeof(SimpleEnum), "Red", "F"));
    }

    private enum SimpleEnum
    {
        Red = 1,
        Blue = 2,
        Green = 3,
        Green_a = 3,
        Green_b = 3,
    }

    private enum ByteEnum : byte
    {
        Min = 0,
        One = 1,
        Two = 2,
        Max = 0xff,
    }

    private enum SByteEnum : sbyte
    {
        Min = -128,
        One = 1,
        Two = 2,
        Max = 127,
    }

    private enum UInt16Enum : ushort
    {
        Min = 0,
        One = 1,
        Two = 2,
        Max = 0xffff,
    }

    private enum Int16Enum : short
    {
        Min = Int16.MinValue,
        One = 1,
        Two = 2,
        Max = 0x7fff,
    }

    private enum UInt32Enum : uint
    {
        Min = 0,
        One = 1,
        Two = 2,
        Max = 0xffffffff,
    }

    private enum Int32Enum : int
    {
        Min = Int32.MinValue,
        One = 1,
        Two = 2,
        Max = 0x7fffffff,
    }

    private enum UInt64Enum : ulong
    {
        Min = 0,
        One = 1,
        Two = 2,
        Max = 0xffffffffffffffff,
    }

    private enum Int64Enum : long
    {
        Min = Int64.MinValue,
        One = 1,
        Two = 2,
        Max = 0x7fffffffffffffff,
    }
}

