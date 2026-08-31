// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class RvaTests
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (!BitConverter.IsLittleEndian)
        {
            // Test is not BE friendly
            return 100;
        }

        int testMethods = 0;
        foreach (MethodInfo mth in typeof(RvaTests)
                     .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                     .Where(m => m.Name.StartsWith("Test_")))
        {
            mth.Invoke(null, null);
            testMethods++;
        }
        return testMethods == 25 ? 100 : -100;
    }

    static ReadOnlySpan<byte> RVA1 => new byte[]
    {
        0x9c, 0x00, 0x01, 0x10,
        0x80, 0xAA, 0xAB, 0xFF,
        0x9b, 0x02, 0x03, 0x14,
        0x85, 0xA6, 0xA7, 0xF9,
    };
    static ReadOnlySpan<sbyte> RVA2 => new sbyte[] { -100, 100, 0, -128, 127 };
    static ReadOnlySpan<bool> RVA3 => new[] { true, false };

    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_1() => AssertEquals<int>((int)RVA1[0], (int)0x9c);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_2() => AssertEquals(RVA1[1], 0x00);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_3() => AssertEquals(RVA1[4], 0x80);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_4() => AssertEquals(RVA1[RVA1.Length - 1], 0xF9);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_5() => ThrowsOOB(() => Consume(RVA1[-1]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_6() => ThrowsOOB(() => Consume(RVA1[0xFF]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_7() => ThrowsOOB(() => Consume(RVA1[RVA1.Length]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_8() => AssertEquals<long>((long)RVA2[0], -100);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_9() => AssertEquals(RVA2[1], 100);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_10() => AssertEquals(RVA2[^1], 127);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_11() => ThrowsOOB(() => Consume(RVA2[-int.MaxValue]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_12() => ThrowsOOB(() => Consume(RVA2[0xFF]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_13() => ThrowsOOB(() => Consume(RVA2[RVA2.Length]));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_14() => AssertEquals(RVA3[0], true);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_15() => AssertEquals(RVA3[1], false);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_16() => AssertEquals<int>(Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 0)), 268501148);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_17() => AssertEquals<uint>(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 1)), 2148532480);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_18() => AssertEquals<int>(Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 10)), -1501228029);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_19() => AssertEquals<short>(Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 0)), 156);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_20() => AssertEquals<ulong>(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 1)), 11240891943721369856);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_21() => AssertEquals<long>(Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 4)), 1441999174721317504);
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_22() => AssertEquals<MyStruct>(Unsafe.ReadUnaligned<MyStruct>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 0)), new MyStruct(-23737906019368804));
    [MethodImpl(MethodImplOptions.NoInlining)] static void Test_23() => AssertEquals<Guid>(Unsafe.ReadUnaligned<Guid>(ref Unsafe.Add(ref MemoryMarshal.GetReference(RVA1), 0)), new Guid("1001009c-aa80-ffab-9b02-031485a6a7f9"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test_24() // AssertProp test
    {
        byte x = RVA1[1];
        if (x > 100)
        {
            Consume(x);
        }
        return x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test_25() // AssertProp test
    {
        sbyte x = RVA2[0];
        if (x > 100)
        {
            Consume(x);
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowsOOB(Action action)
    {
        try
        {
            action();
        }
        catch (IndexOutOfRangeException)
        {
            return;
        }
        throw new InvalidOperationException("IndexOutOfRangeException was expected");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertEquals<T>(T actual, T expected, [CallerLineNumber] int line = 0)
    {
        if (!actual.Equals(expected))
        {
            throw new InvalidOperationException($"Line:{line}  actual={actual}, expected={expected}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume<T>(T _) { }

    public record struct MyStruct(long a);
}
