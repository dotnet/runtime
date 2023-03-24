// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class CompareVectorWithZero
{
    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void AllTests()
    {
        Test(Vector128.Create(0));
        Test(Vector128.Create(0.0f));
        Test(Vector128.Create(-0.0f));
        Test(Vector128.Create(0.0));
        Test(Vector128.Create(-0.0));
        
        TestReversed(Vector128.Create(0));
        TestReversed(Vector128.Create(0.0f));
        TestReversed(Vector128.Create(-0.0f));
        TestReversed(Vector128.Create(0.0));
        TestReversed(Vector128.Create(-0.0));

        Test(Vector128.Create(-10));
        Test(Vector128.Create(10));
        Test(Vector128.Create((sbyte)-10));
        Test(Vector128.Create((ushort)10));
        Test(Vector64.Create(0));
        Test(Vector64.Create(0.0f));
        Test(Vector64.Create(-0.0f));
        Test(Vector64.Create(0.0));
        Test(Vector64.Create(-0.0));
        Test(Vector64.Create(-10));
        Test(Vector64.Create(10));
        Test(Vector64.Create((sbyte)-10));
        Test(Vector64.Create((ushort)10));
        Test(Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1));
        Test(Vector128.Create(0, 0, 0, 0, 0, 0, 0, -1));
        Test(Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1));
        Test(Vector64.Create(0, 0, 0, 0, 0, 0, 0, -1));

        Test(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));
        Test(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));
        Test(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));
        Test(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));

        Test(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        Test(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));
        Test(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        Test(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));

        TestReversed(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestReversed(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));
        TestReversed(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestReversed(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ToVar<T>(T t) => t;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertTrue(bool expr)
    {
        if (!expr)
            throw new InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((v == Vector128<T>.Zero) == 
                   (v == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((v == Vector64<T>.Zero) == 
                   (v == Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReversed<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((Vector128<T>.Zero == v) == 
                   (v == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReversed<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((Vector64<T>.Zero == v) == 
                   (v == Vector64.Create(ToVar(default(T)))));
}
