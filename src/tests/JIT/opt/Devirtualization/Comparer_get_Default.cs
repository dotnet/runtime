// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    private static int s_ReturnCode = 100;

    private static void AssertEquals<T>(T expected, T actual, string values = "", [CallerLineNumber] int line = 0)
    {
        if (!expected.Equals(actual))
        {
            Console.WriteLine($"{values}{expected} != {actual}, L{line}");
            s_ReturnCode++;
        }
    }

    private static void AssertThrows<TException>(Action action, string values = "", [CallerLineNumber] int line = 0) where TException : Exception
    {
        try
        {
            action();
            Console.WriteLine($"{values}no {typeof(TException).FullName}, L{line}");
            s_ReturnCode++;
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
                return;
            Console.WriteLine($"{values}{ex.GetType().FullName} != {typeof(TException).FullName}, L{line}");
            s_ReturnCode++;
        }
    }

    private static void Compare_Boolean(Boolean a, Boolean b) =>
        AssertEquals(a.CompareTo(b), Comparer<Boolean>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Byte(Byte a, Byte b) =>
        AssertEquals(a.CompareTo(b), Comparer<Byte>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_SByte(SByte a, SByte b) =>
        AssertEquals(a.CompareTo(b), Comparer<SByte>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Char(Char a, Char b) =>
        AssertEquals(a.CompareTo(b), Comparer<Char>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_UInt16(UInt16 a, UInt16 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt16>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Int16(Int16 a, Int16 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int16>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_UInt32(UInt32 a, UInt32 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt32>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Int32(Int32 a, Int32 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int32>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Int64(Int64 a, Int64 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int64>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_UInt64(UInt64 a, UInt64 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt64>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_IntPtr(IntPtr a, IntPtr b) =>
        AssertEquals(a.CompareTo(b), Comparer<IntPtr>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_UIntPtr(UIntPtr a, UIntPtr b) =>
        AssertEquals(a.CompareTo(b), Comparer<UIntPtr>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_nint(nint a, nint b) =>
        AssertEquals(a.CompareTo(b), Comparer<nint>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_nuint(nuint a, nuint b) =>
        AssertEquals(a.CompareTo(b), Comparer<nuint>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Enum_Int32(MethodImplOptions a, MethodImplOptions b) =>
        AssertEquals(a.CompareTo(b), Comparer<MethodImplOptions>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Enum_Byte(Enum_byte a, Enum_byte b) =>
        AssertEquals(a.CompareTo(b), Comparer<Enum_byte>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_String(String a, String b) =>
        AssertEquals(a.CompareTo(b), Comparer<String>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_DateTime(DateTime a, DateTime b) =>
        AssertEquals(a.CompareTo(b), Comparer<DateTime>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Int32_Nullable(long? a, long? b)
    {
        int actual = Comparer<long?>.Default.Compare(a, b);
        int expected = 0;
        if (a.HasValue)
            expected = b.HasValue ? a.Value.CompareTo(b.Value) : 1;
        else
            expected = b.HasValue ? -1 : 0;
        AssertEquals(expected, actual, $"({a}; {b}): ");
    }

    private static void Compare_Enum_Int32_Nullable(MethodImplOptions? a, MethodImplOptions? b)
    {
        int actual = Comparer<MethodImplOptions?>.Default.Compare(a, b);
        int expected = 0;
        if (a.HasValue)
            expected = b.HasValue ? a.Value.CompareTo(b.Value) : 1;
        else
            expected = b.HasValue ? -1 : 0;
        AssertEquals(expected, actual, $"({a}; {b}): ");
    }

    private static void Compare_Struct1(Struct1 a, Struct1 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Struct1>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Struct2(Struct2 a, Struct2 b) =>
        AssertThrows<ArgumentException>(() => Comparer<Struct2>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Struct1_Nullable(Struct1? a, Struct1? b) =>
        AssertEquals(((IComparable)a)?.CompareTo(b) ?? (b.HasValue ? -1 : 0), Comparer<Struct1?>.Default.Compare(a, b), $"({a}; {b}): ");

    private static void Compare_Struct2_Nullable(Struct2? a, Struct2? b)
    {
        if (!a.HasValue && !b.HasValue)
            AssertEquals(0, Comparer<Struct2?>.Default.Compare(a, b), $"({a}; {b}): ");
        else if (!a.HasValue)
            AssertEquals(-1, Comparer<Struct2?>.Default.Compare(a, b), $"({a}; {b}): ");
        else if (!b.HasValue)
            AssertEquals(1, Comparer<Struct2?>.Default.Compare(a, b), $"({a}; {b}): ");
        else
            AssertThrows<ArgumentException>(() => Comparer<Struct2?>.Default.Compare(a, b));
    }

    private static string PrintBits<T>(T value)
    {
        ulong l = 0;
        Unsafe.As<ulong, T>(ref l) = value;
        return $"({typeof(T).FullName}){l}";
    }

    private static void Compare_Double_Enum(DoubleEnum a, DoubleEnum b) =>
        AssertEquals(a.CompareTo(b), Comparer<DoubleEnum>.Default.Compare(a, b), $"({PrintBits(a)}; {PrintBits(b)}): ");
    
    private static void Compare_Generic_Enum<TEnum>(TEnum a, TEnum b) where TEnum : Enum =>
        AssertEquals(a.CompareTo(b), Comparer<TEnum>.Default.Compare(a, b), $"({PrintBits(a)}; {PrintBits(b)}): ");

    [Fact]
    public static int TestEntryPoint()
    {
        long[] values =
            {
                -2, -1, 0, 1, 2,
                sbyte.MinValue - 1, sbyte.MinValue, sbyte.MinValue + 1,
                sbyte.MaxValue - 1, sbyte.MaxValue, sbyte.MaxValue + 1,
                byte.MaxValue - 1, byte.MaxValue, byte.MaxValue + 1,
                short.MinValue, short.MinValue + 1,
                short.MaxValue - 1, short.MaxValue, short.MaxValue + 1,
                ushort.MaxValue - 1, ushort.MaxValue, ushort.MaxValue + 1,
                int.MinValue, int.MinValue + 1,
                int.MaxValue - 1, int.MaxValue, int.MaxValue + 1L,
                uint.MaxValue - 1, uint.MaxValue, uint.MaxValue + 1L,
                long.MinValue, long.MinValue + 1,
                long.MaxValue - 1, long.MaxValue, BitConverter.DoubleToInt64Bits(double.NaN)
            };

        for (var i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                long a = values[i];
                long b = values[j];

                var boolA = Unsafe.As<long, bool>(ref a);
                var boolB = Unsafe.As<long, bool>(ref b);
                Compare_Boolean(boolA, boolB);

                var byteA = Unsafe.As<long, byte>(ref a);
                var byteB = Unsafe.As<long, byte>(ref b);
                Compare_Byte(byteA, byteB);

                var sbyteA = Unsafe.As<long, sbyte>(ref a);
                var sbyteB = Unsafe.As<long, sbyte>(ref b);
                Compare_SByte(sbyteA, sbyteB);

                var shortA = Unsafe.As<long, short>(ref a);
                var shortB = Unsafe.As<long, short>(ref b);
                Compare_Int16(shortA, shortB);

                var ushortA = Unsafe.As<long, ushort>(ref a);
                var ushortB = Unsafe.As<long, ushort>(ref b);
                Compare_UInt16(ushortA, ushortB);

                var charA = Unsafe.As<long, char>(ref a);
                var charB = Unsafe.As<long, char>(ref b);
                Compare_Char(charA, charB);

                var intA = Unsafe.As<long, int>(ref a);
                var intB = Unsafe.As<long, int>(ref b);
                Compare_Int32(intA, intB);

                var uintA = Unsafe.As<long, uint>(ref a);
                var uintB = Unsafe.As<long, uint>(ref b);
                Compare_UInt32(uintA, uintB);

                var longA = Unsafe.As<long, long>(ref a);
                var longB = Unsafe.As<long, long>(ref b);
                Compare_Int64(longA, longB);

                var ulongA = Unsafe.As<long, ulong>(ref a);
                var ulongB = Unsafe.As<long, ulong>(ref b);
                Compare_UInt64(ulongA, ulongB);

                var intPtrA = Unsafe.As<long, IntPtr>(ref a);
                var intPtrB = Unsafe.As<long, IntPtr>(ref b);
                Compare_IntPtr(intPtrA, intPtrB);

                var uintPtrA = Unsafe.As<long, UIntPtr>(ref a);
                var uintPtrB = Unsafe.As<long, UIntPtr>(ref b);
                Compare_UIntPtr(uintPtrA, uintPtrB);

                var nintA = Unsafe.As<long, nint>(ref a);
                var nintB = Unsafe.As<long, nint>(ref b);
                Compare_nint(nintA, nintB);

                var nuintA = Unsafe.As<long, nuint>(ref a);
                var nuintB = Unsafe.As<long, nuint>(ref b);
                Compare_nuint(nuintA, nuintB);

                var enumIntA = Unsafe.As<long, MethodImplOptions>(ref a);
                var enumIntB = Unsafe.As<long, MethodImplOptions>(ref b);
                Compare_Enum_Int32(enumIntA, enumIntB);

                var enumByteA = Unsafe.As<long, Enum_byte>(ref a);
                var enumByteB = Unsafe.As<long, Enum_byte>(ref b);
                Compare_Enum_Byte(enumByteA, enumByteB);

                Compare_DateTime(
                    new DateTime(Math.Clamp(a, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)),
                    new DateTime(Math.Clamp(b, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)));

                Compare_Int32_Nullable(a, b);
                Compare_Enum_Int32_Nullable(enumIntA, enumIntB);

                Compare_Int32_Nullable(null, b);
                Compare_Enum_Int32_Nullable(null, enumIntB);

                Compare_Int32_Nullable(a, null);
                Compare_Enum_Int32_Nullable(enumIntA, null);

                var structA = new Struct1 {a = a, b = b};
                var structB = new Struct1 {a = b, b = a};
                Compare_Struct1(structA, structB);

                var struct2A = new Struct2 {a = a, b = b};
                var struct2B = new Struct2 {a = b, b = a};
                Compare_Struct2(struct2A, struct2B);

                Compare_Struct1_Nullable(structA, structB);
                Compare_Struct2_Nullable(struct2A, struct2B);

                Compare_Struct1_Nullable(null, structB);
                Compare_Struct2_Nullable(null, struct2B);

                Compare_Struct1_Nullable(structA, null);
                Compare_Struct2_Nullable(struct2A, null);

                Compare_Struct1_Nullable(null, null);
                Compare_Struct2_Nullable(null, null);

                if (TestLibrary.PlatformDetection.IsRareEnumsSupported)
                {
                    // workaround for: https://github.com/dotnet/roslyn/issues/68770
                    static T Bitcast<T>(long l) => Unsafe.As<long, T>(ref l);

                    var enumCharA = Bitcast<CharEnum>(a);
                    var enumCharB = Bitcast<CharEnum>(b);
                    Compare_Generic_Enum(enumCharA, enumCharB);

                    var enumBoolA = Bitcast<BoolEnum>(a);
                    var enumBoolB = Bitcast<BoolEnum>(b);
                    Compare_Generic_Enum(enumBoolA, enumBoolB);

                    var enumFloatA = Bitcast<FloatEnum>(a);
                    var enumFloatB = Bitcast<FloatEnum>(b);
                    Compare_Generic_Enum(enumFloatA, enumFloatB);

                    var enumDoubleA = Bitcast<DoubleEnum>(a);
                    var enumDoubleB = Bitcast<DoubleEnum>(b);
                    Compare_Generic_Enum(enumDoubleA, enumDoubleB);
                    Compare_Double_Enum(enumDoubleA, enumDoubleB);

                    var enumIntPtrA = Bitcast<IntPtrEnum>(a);
                    var enumIntPtrB = Bitcast<IntPtrEnum>(b);
                    Compare_Generic_Enum(enumIntPtrA, enumIntPtrB);

                    var enumUIntPtrA = Bitcast<UIntPtrEnum>(a);
                    var enumUIntPtrB = Bitcast<UIntPtrEnum>(b);
                    Compare_Generic_Enum(enumUIntPtrA, enumUIntPtrB);
                }
            }
        }

        string[] strings = { "", "0", "00", "1", "11", "111", "\u043F\u0440\u0438\u0432\u0435\u0442", "Hello" };
        foreach (var str1 in strings)
        {
            foreach (var str2 in strings)
            {
                Compare_String(str1, str2);
                Compare_String(str2, str1);
            }
        }

        Compare_Int32_Nullable(null, 0);
        Compare_Int32_Nullable(0, null);
        Compare_Int32_Nullable(null, null);
        Compare_Int32_Nullable(null, 1);
        Compare_Int32_Nullable(1, null);
        Compare_Int32_Nullable(null, -1);
        Compare_Int32_Nullable(-1, null);
        Compare_Enum_Int32_Nullable(null, null);

        GenericsTest();
        GetTypeTests();
        GetHashCodeTests();

        return s_ReturnCode;
    }

    private static void GenericsTest()
    {
        AssertEquals(true, Test<string, object>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Test<T,U>()
        {
            return EqualityComparer<G<T,U>>.Default.Equals(default, default);
        }
    }

    private static void GetTypeTests()
    {
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.Byte]", Comparer<byte>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.Int32]", Comparer<int>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.String]", Comparer<string>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.Guid]", Comparer<Guid>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[System.Runtime.CompilerServices.MethodImplOptions]", Comparer<MethodImplOptions>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[CharEnum]", Comparer<CharEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[BoolEnum]", Comparer<BoolEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[FloatEnum]", Comparer<FloatEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[DoubleEnum]", Comparer<DoubleEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[IntPtrEnum]", Comparer<IntPtrEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[UIntPtrEnum]", Comparer<UIntPtrEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[Struct1]", Comparer<Struct1>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[Struct2]", Comparer<Struct2>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[StructGeneric`1[System.Int32]]", Comparer<StructGeneric<int>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[StructGeneric`1[System.String]]", Comparer<StructGeneric<string>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[StructGenericString`1[System.String]]", Comparer<StructGenericString<string>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[StructGenericString`1[System.Object]]", Comparer<StructGenericString<object>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[StructGeneric`1[StructGeneric`1[System.Int32]]]", Comparer<StructGeneric<StructGeneric<int>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[StructGeneric`1[StructGeneric`1[System.String]]]", Comparer<StructGeneric<StructGeneric<string>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[StructGenericString`1[StructGeneric`1[System.String]]]", Comparer<StructGenericString<StructGeneric<string>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[StructGenericString`1[StructGeneric`1[System.Object]]]", Comparer<StructGenericString<StructGeneric<object>>>.Default.GetType().ToString());
        
        AssertEquals("System.Collections.Generic.NullableComparer`1[System.Byte]", Comparer<byte?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[System.Int32]", Comparer<int?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[System.Runtime.CompilerServices.MethodImplOptions]", Comparer<MethodImplOptions?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[CharEnum]", Comparer<CharEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[BoolEnum]", Comparer<BoolEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[FloatEnum]", Comparer<FloatEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[DoubleEnum]", Comparer<DoubleEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[IntPtrEnum]", Comparer<IntPtrEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[UIntPtrEnum]", Comparer<UIntPtrEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[Struct1]", Comparer<Struct1?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[Struct2]", Comparer<Struct2?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGeneric`1[System.Int32]]", Comparer<StructGeneric<int>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGeneric`1[System.String]]", Comparer<StructGeneric<string>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGenericString`1[System.String]]", Comparer<StructGenericString<string>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGenericString`1[System.Object]]", Comparer<StructGenericString<object>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGeneric`1[StructGeneric`1[System.Int32]]]", Comparer<StructGeneric<StructGeneric<int>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGeneric`1[StructGeneric`1[System.String]]]", Comparer<StructGeneric<StructGeneric<string>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGenericString`1[StructGeneric`1[System.String]]]", Comparer<StructGenericString<StructGeneric<string>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[StructGenericString`1[StructGeneric`1[System.Object]]]", Comparer<StructGenericString<StructGeneric<object>>?>.Default.GetType().ToString());

        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[System.Byte]", EqualityComparer<byte>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[System.Int32]", EqualityComparer<int>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.StringEqualityComparer", EqualityComparer<string>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[System.Guid]", EqualityComparer<Guid>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[System.Runtime.CompilerServices.MethodImplOptions]", EqualityComparer<MethodImplOptions>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[CharEnum]", EqualityComparer<CharEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[BoolEnum]", EqualityComparer<BoolEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[FloatEnum]", EqualityComparer<FloatEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[DoubleEnum]", EqualityComparer<DoubleEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[IntPtrEnum]", EqualityComparer<IntPtrEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumEqualityComparer`1[UIntPtrEnum]", EqualityComparer<UIntPtrEnum>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[Struct1]", EqualityComparer<Struct1>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[Struct2]", EqualityComparer<Struct2>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[StructGeneric`1[System.Int32]]", EqualityComparer<StructGeneric<int>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[StructGeneric`1[System.String]]", EqualityComparer<StructGeneric<string>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[StructGenericString`1[System.String]]", EqualityComparer<StructGenericString<string>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[StructGenericString`1[System.Object]]", EqualityComparer<StructGenericString<object>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[StructGeneric`1[StructGeneric`1[System.Int32]]]", EqualityComparer<StructGeneric<StructGeneric<int>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericEqualityComparer`1[StructGeneric`1[StructGeneric`1[System.String]]]", EqualityComparer<StructGeneric<StructGeneric<string>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[StructGenericString`1[StructGeneric`1[System.String]]]", EqualityComparer<StructGenericString<StructGeneric<string>>>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectEqualityComparer`1[StructGenericString`1[StructGeneric`1[System.Object]]]", EqualityComparer<StructGenericString<StructGeneric<object>>>.Default.GetType().ToString());
        
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[System.Byte]", EqualityComparer<byte?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[System.Int32]", EqualityComparer<int?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[System.Runtime.CompilerServices.MethodImplOptions]", EqualityComparer<MethodImplOptions?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[CharEnum]", EqualityComparer<CharEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[BoolEnum]", EqualityComparer<BoolEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[FloatEnum]", EqualityComparer<FloatEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[DoubleEnum]", EqualityComparer<DoubleEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[IntPtrEnum]", EqualityComparer<IntPtrEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[UIntPtrEnum]", EqualityComparer<UIntPtrEnum?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[Struct1]", EqualityComparer<Struct1?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[Struct2]", EqualityComparer<Struct2?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGeneric`1[System.Int32]]", EqualityComparer<StructGeneric<int>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGeneric`1[System.String]]", EqualityComparer<StructGeneric<string>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGenericString`1[System.String]]", EqualityComparer<StructGenericString<string>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGenericString`1[System.Object]]", EqualityComparer<StructGenericString<object>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGeneric`1[StructGeneric`1[System.Int32]]]", EqualityComparer<StructGeneric<StructGeneric<int>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGeneric`1[StructGeneric`1[System.String]]]", EqualityComparer<StructGeneric<StructGeneric<string>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGenericString`1[StructGeneric`1[System.String]]]", EqualityComparer<StructGenericString<StructGeneric<string>>?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableEqualityComparer`1[StructGenericString`1[StructGeneric`1[System.Object]]]", EqualityComparer<StructGenericString<StructGeneric<object>>?>.Default.GetType().ToString());
    }
    private static int GetHashCodeTests()
    {
        // Just to make sure it doesn't crash
        return Comparer<int>.Default.GetHashCode() +
               Comparer<string>.Default.GetHashCode() +
               Comparer<MethodImplOptions>.Default.GetHashCode() +
               Comparer<MethodImplOptions?>.Default.GetHashCode() +
               Comparer<byte?>.Default.GetHashCode() +
               Comparer<Guid>.Default.GetHashCode() +
               Comparer<Struct1>.Default.GetHashCode() +
               Comparer<Struct2>.Default.GetHashCode() +
               Comparer<Struct1?>.Default.GetHashCode() +
               Comparer<Struct2?>.Default.GetHashCode();
    }
}

public enum Enum_byte : byte
{
    A,B,C,D,E
}

public struct Struct1 : IComparable
{
    public long a;
    public long b;
    public int CompareTo(object obj)
    {
        return obj is Struct1 str ?  b.CompareTo(str.b) : 1;
    }
}

public struct Struct2
{
    public long a;
    public long b;
}

public struct StructGeneric<T> : IEquatable<StructGeneric<T>>, IComparable<StructGeneric<T>>
{
    public T t;
    
    public bool Equals(StructGeneric<T> s) => EqualityComparer<T>.Default.Equals(t, s.t);
    public int CompareTo(StructGeneric<T> s) => Comparer<T>.Default.Compare(t, s.t);
}

public struct StructGenericString<T> : IEquatable<string>, IComparable<string>
{
    public T t;
    
    public bool Equals(string s) => s == t?.ToString();
    public int CompareTo(string s) => Comparer<string>.Default.Compare(t?.ToString(), s);
}

struct G<T,U> : IEquatable<G<U,T>>
{
    public bool Equals(G<U,T> x) => false;
}
