// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class Program
{
    private static int s_ReturnCode = 100;

    private static void AssertEquals(int expected, int actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
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

    private static void Compare_Enum(MethodImplOptions a, MethodImplOptions b) =>
        AssertEquals(a.CompareTo(b), Comparer<MethodImplOptions>.Default.Compare(a, b));

    private static void Compare_String(String a, String b) =>
        AssertEquals(a.CompareTo(b), Comparer<String>.Default.Compare(a, b));

    private static void Compare_DateTime(DateTime a, DateTime b) =>
        AssertEquals(a.CompareTo(b), Comparer<DateTime>.Default.Compare(a, b));

    public static int Main(string[] args)
    {
        long[] values = {1, 2, 3, long.MaxValue};

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

                var enumA = Unsafe.As<long, MethodImplOptions>(ref a);
                var enumB = Unsafe.As<long, MethodImplOptions>(ref b);
                Compare_Enum(enumA, enumB);
                
                Compare_DateTime(
                    new DateTime(Math.Clamp(a, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)), 
                    new DateTime(Math.Clamp(b, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)));
            }
        }

        string[] strings = {"", "0", "00", "1", "11", "111", "привет", "Hello"};
        foreach (var str1 in strings)
        {
            foreach (var str2 in strings)
            {
                Compare_String(str1, str2);
                Compare_String(str2, str1);
            }
        }

        return s_ReturnCode;
    }
}
