// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134475
{
    [Fact]
    public static void TestEntryPoint()
    {
        nint handle = GetHandle();
        foreach (nint value in new nint[] { 0, 1, -1, 123 })
        {
            Assert.Equal(value + handle + 8, AddAfterHandle(value));
            Assert.Equal(value + 8 + handle, AddBeforeHandle(value));
            Assert.Equal(value ^ handle ^ 8, XorHandle(value));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint GetHandle() => RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint AddAfterHandle(nint value) =>
        (value + RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle)) + 8;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint AddBeforeHandle(nint value) =>
        (value + 8) + RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint XorHandle(nint value) =>
        (value ^ RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle)) ^ 8;
}
