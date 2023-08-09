// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class UnrollSequenceEqualTests
{
    [Fact]
    public static int TestEntryPoint()
    {
        var testMethods = typeof(UnrollSequenceEqualTests)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name.StartsWith("Test"));

        foreach (MethodInfo testMethod in testMethods)
            if (!(bool)testMethod.Invoke(null, new object[] { "0123456789ABCDEF0"u8.ToArray() }))
                throw new InvalidOperationException($"{testMethod.Name} returned false.");

        foreach (MethodInfo testMethod in testMethods)
            if ((bool)testMethod.Invoke(null, new object[] { "123456789ABCDEF01"u8.ToArray() }))
                throw new InvalidOperationException($"{testMethod.Name} returned true.");

        return 100;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test1(byte[] data) => data.AsSpan().StartsWith("0"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test2(byte[] data) => data.AsSpan().StartsWith("01"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test3(byte[] data) => data.AsSpan().StartsWith("012"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test4(byte[] data) => data.AsSpan().StartsWith("0123"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test5(byte[] data) => data.AsSpan().StartsWith("01234"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test6(byte[] data) => data.AsSpan().StartsWith("012345"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test7(byte[] data) => data.AsSpan().StartsWith("0123456"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test8(byte[] data) => data.AsSpan().StartsWith("01234567"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test9(byte[] data) => data.AsSpan().StartsWith("012345678"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test10(byte[] data) => data.AsSpan().StartsWith("0123456789"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test11(byte[] data) => data.AsSpan().StartsWith("0123456789A"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test12(byte[] data) => data.AsSpan().StartsWith("0123456789AB"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test13(byte[] data) => data.AsSpan().StartsWith("0123456789ABC"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test14(byte[] data) => data.AsSpan().StartsWith("0123456789ABCD"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test15(byte[] data) => data.AsSpan().StartsWith("0123456789ABCDE"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test16(byte[] data) => data.AsSpan().StartsWith("0123456789ABCDEF"u8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static bool Test17(byte[] data) => data.AsSpan().StartsWith("0123456789ABCDEF0"u8);
}
