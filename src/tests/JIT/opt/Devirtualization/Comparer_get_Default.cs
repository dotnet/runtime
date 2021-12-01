// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

class Program
{
    private static int s_ReturnCode = 100;

    private static void AssertEquals<T>(T expected, T actual, [CallerLineNumber] int line = 0)
    {
        if (!expected.Equals(actual))
        {
            Console.WriteLine($"{expected} != {actual}, L{line}");
            s_ReturnCode++;
        }
    }

    private static void Compare_Boolean(Boolean a, Boolean b) =>
        AssertEquals(a.CompareTo(b), Comparer<Boolean>.Default.Compare(a, b));

    private static void Compare_Byte(Byte a, Byte b) =>
        AssertEquals(a.CompareTo(b), Comparer<Byte>.Default.Compare(a, b));

    private static void Compare_SByte(SByte a, SByte b) =>
        AssertEquals(a.CompareTo(b), Comparer<SByte>.Default.Compare(a, b));

    private static void Compare_Char(Char a, Char b) =>
        AssertEquals(a.CompareTo(b), Comparer<Char>.Default.Compare(a, b));

    private static void Compare_UInt16(UInt16 a, UInt16 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt16>.Default.Compare(a, b));

    private static void Compare_Int16(Int16 a, Int16 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int16>.Default.Compare(a, b));

    private static void Compare_UInt32(UInt32 a, UInt32 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt32>.Default.Compare(a, b));

    private static void Compare_Int32(Int32 a, Int32 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int32>.Default.Compare(a, b));

    private static void Compare_Int64(Int64 a, Int64 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Int64>.Default.Compare(a, b));

    private static void Compare_UInt64(UInt64 a, UInt64 b) =>
        AssertEquals(a.CompareTo(b), Comparer<UInt64>.Default.Compare(a, b));

    private static void Compare_IntPtr(IntPtr a, IntPtr b) =>
        AssertEquals(a.CompareTo(b), Comparer<IntPtr>.Default.Compare(a, b));

    private static void Compare_UIntPtr(UIntPtr a, UIntPtr b) =>
        AssertEquals(a.CompareTo(b), Comparer<UIntPtr>.Default.Compare(a, b));

    private static void Compare_nint(nint a, nint b) =>
        AssertEquals(a.CompareTo(b), Comparer<nint>.Default.Compare(a, b));

    private static void Compare_nuint(nuint a, nuint b) =>
        AssertEquals(a.CompareTo(b), Comparer<nuint>.Default.Compare(a, b));

    private static void Compare_Enum_Int32(MethodImplOptions a, MethodImplOptions b) =>
        AssertEquals(a.CompareTo(b), Comparer<MethodImplOptions>.Default.Compare(a, b));

    private static void Compare_Enum_Byte(Enum_byte a, Enum_byte b) =>
        AssertEquals(a.CompareTo(b), Comparer<Enum_byte>.Default.Compare(a, b));

    private static void Compare_String(String a, String b) =>
        AssertEquals(a.CompareTo(b), Comparer<String>.Default.Compare(a, b));

    private static void Compare_DateTime(DateTime a, DateTime b) =>
        AssertEquals(a.CompareTo(b), Comparer<DateTime>.Default.Compare(a, b));

    private static void Compare_Struct1(Struct1 a, Struct1 b) =>
        AssertEquals(a.CompareTo(b), Comparer<Struct1>.Default.Compare(a, b));

    private static void Compare_Int32_Nullable(long? a, long? b)
    {
        int actual = Comparer<long?>.Default.Compare(a, b);
        int expected = 0;
        if (a.HasValue)
            expected = b.HasValue ? a.Value.CompareTo(b.Value) : 1;
        else
            expected = b.HasValue ? -1 : 0;
        AssertEquals(expected, actual);
    }

    public static int Main(string[] args)
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
                long.MaxValue - 1, long.MaxValue
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

                var structA = new Struct1 {a = a, b = b};
                var structB = new Struct1 {a = b, b = a};
                Compare_Struct1(structA, structB);

                Compare_DateTime(
                    new DateTime(Math.Clamp(a, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)),
                    new DateTime(Math.Clamp(b, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)));

                Compare_Int32_Nullable(a, b);
            }
        }

        string[] strings = { "", "0", "00", "1", "11", "111", "привет", "Hello" };
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

        GetTypeTests();
        GetHashCodeTests();

        return s_ReturnCode;
    }

    private static void GetTypeTests()
    {
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.Int32]", Comparer<int>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.String]", Comparer<string>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.GenericComparer`1[System.Guid]", Comparer<Guid>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.EnumComparer`1[System.Runtime.CompilerServices.MethodImplOptions]", Comparer<MethodImplOptions>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.NullableComparer`1[System.Byte]", Comparer<byte?>.Default.GetType().ToString());
        AssertEquals("System.Collections.Generic.ObjectComparer`1[Struct1]", Comparer<Struct1>.Default.GetType().ToString());
    }
    private static int GetHashCodeTests()
    {
        // Just to make sure it doesn't crash
        return Comparer<int>.Default.GetHashCode() +
               Comparer<string>.Default.GetHashCode() +
               Comparer<MethodImplOptions>.Default.GetHashCode() +
               Comparer<byte?>.Default.GetHashCode() +
               Comparer<Guid>.Default.GetHashCode() +
               Comparer<Struct1>.Default.GetHashCode();
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
        return b.CompareTo(((Struct1) obj).b);
    }
}
