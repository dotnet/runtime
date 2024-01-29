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
    public static void TestVector64Equality()
    {
        TestEquality(Vector64.Create(0));
        TestEquality(Vector64.Create(0.0f));
        TestEquality(Vector64.Create(-0.0f));
        TestEquality(Vector64.Create(0.0));
        TestEquality(Vector64.Create(-0.0));

        TestEqualityUsingReversedInputs(Vector64.Create(0));
        TestEqualityUsingReversedInputs(Vector64.Create(0.0f));
        TestEqualityUsingReversedInputs(Vector64.Create(-0.0f));
        TestEqualityUsingReversedInputs(Vector64.Create(0.0));
        TestEqualityUsingReversedInputs(Vector64.Create(-0.0));

        TestEquality(Vector64.Create(-10));
        TestEquality(Vector64.Create(10));
        TestEquality(Vector64.Create((sbyte)-10));
        TestEquality(Vector64.Create((ushort)10));
        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector64.Create(0), Vector64.Create(0));
        TestEqualityUsingAnd(Vector64.Create(0.0f), Vector64.Create(0.0f));
        TestEqualityUsingAnd(Vector64.Create(-0.0f), Vector64.Create(-0.0f));
        TestEqualityUsingAnd(Vector64.Create(0.0), Vector64.Create(0.0));
        TestEqualityUsingAnd(Vector64.Create(-0.0), Vector64.Create(-0.0));

        TestEqualityUsingAndNot(Vector64.Create(0), Vector64.Create(0));
        TestEqualityUsingAndNot(Vector64.Create(0.0f), Vector64.Create(0.0f));
        TestEqualityUsingAndNot(Vector64.Create(-0.0f), Vector64.Create(-0.0f));
        TestEqualityUsingAndNot(Vector64.Create(0.0), Vector64.Create(0.0));
        TestEqualityUsingAndNot(Vector64.Create(-0.0), Vector64.Create(-0.0));

        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));
        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));

        TestEquality(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestEquality(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));

        TestEqualityUsingReversedInputs(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestEqualityUsingReversedInputs(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector128Equality()
    {
        TestEquality(Vector128.Create(0));
        TestEquality(Vector128.Create(0.0f));
        TestEquality(Vector128.Create(-0.0f));
        TestEquality(Vector128.Create(0.0));
        TestEquality(Vector128.Create(-0.0));

        TestEqualityUsingReversedInputs(Vector128.Create(0));
        TestEqualityUsingReversedInputs(Vector128.Create(0.0f));
        TestEqualityUsingReversedInputs(Vector128.Create(-0.0f));
        TestEqualityUsingReversedInputs(Vector128.Create(0.0));
        TestEqualityUsingReversedInputs(Vector128.Create(-0.0));

        TestEquality(Vector128.Create(-10));
        TestEquality(Vector128.Create(10));
        TestEquality(Vector128.Create((sbyte)-10));
        TestEquality(Vector128.Create((ushort)10));
        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector128.Create(0), Vector128.Create(0));
        TestEqualityUsingAnd(Vector128.Create(0.0f), Vector128.Create(0.0f));
        TestEqualityUsingAnd(Vector128.Create(-0.0f), Vector128.Create(-0.0f));
        TestEqualityUsingAnd(Vector128.Create(0.0), Vector128.Create(0.0));
        TestEqualityUsingAnd(Vector128.Create(-0.0), Vector128.Create(-0.0));

        TestEqualityUsingAndNot(Vector128.Create(0), Vector128.Create(0));
        TestEqualityUsingAndNot(Vector128.Create(0.0f), Vector128.Create(0.0f));
        TestEqualityUsingAndNot(Vector128.Create(-0.0f), Vector128.Create(-0.0f));
        TestEqualityUsingAndNot(Vector128.Create(0.0), Vector128.Create(0.0));
        TestEqualityUsingAndNot(Vector128.Create(-0.0), Vector128.Create(-0.0));

        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));
        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));

        TestEquality(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestEquality(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));

        TestEqualityUsingReversedInputs(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestEqualityUsingReversedInputs(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector256Equality()
    {
        TestEquality(Vector256.Create(0));
        TestEquality(Vector256.Create(0.0f));
        TestEquality(Vector256.Create(-0.0f));
        TestEquality(Vector256.Create(0.0));
        TestEquality(Vector256.Create(-0.0));

        TestEqualityUsingReversedInputs(Vector256.Create(0));
        TestEqualityUsingReversedInputs(Vector256.Create(0.0f));
        TestEqualityUsingReversedInputs(Vector256.Create(-0.0f));
        TestEqualityUsingReversedInputs(Vector256.Create(0.0));
        TestEqualityUsingReversedInputs(Vector256.Create(-0.0));

        TestEquality(Vector256.Create(-10));
        TestEquality(Vector256.Create(10));
        TestEquality(Vector256.Create((sbyte)-10));
        TestEquality(Vector256.Create((ushort)10));
        TestEquality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector256.Create(0), Vector256.Create(0));
        TestEqualityUsingAnd(Vector256.Create(0.0f), Vector256.Create(0.0f));
        TestEqualityUsingAnd(Vector256.Create(-0.0f), Vector256.Create(-0.0f));
        TestEqualityUsingAnd(Vector256.Create(0.0), Vector256.Create(0.0));
        TestEqualityUsingAnd(Vector256.Create(-0.0), Vector256.Create(-0.0));

        TestEqualityUsingAndNot(Vector256.Create(0), Vector256.Create(0));
        TestEqualityUsingAndNot(Vector256.Create(0.0f), Vector256.Create(0.0f));
        TestEqualityUsingAndNot(Vector256.Create(-0.0f), Vector256.Create(-0.0f));
        TestEqualityUsingAndNot(Vector256.Create(0.0), Vector256.Create(0.0));
        TestEqualityUsingAndNot(Vector256.Create(-0.0), Vector256.Create(-0.0));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector512Equality()
    {
        TestEquality(Vector512.Create(0));
        TestEquality(Vector512.Create(0.0f));
        TestEquality(Vector512.Create(-0.0f));
        TestEquality(Vector512.Create(0.0));
        TestEquality(Vector512.Create(-0.0));

        TestEqualityUsingReversedInputs(Vector512.Create(0));
        TestEqualityUsingReversedInputs(Vector512.Create(0.0f));
        TestEqualityUsingReversedInputs(Vector512.Create(-0.0f));
        TestEqualityUsingReversedInputs(Vector512.Create(0.0));
        TestEqualityUsingReversedInputs(Vector512.Create(-0.0));

        TestEquality(Vector512.Create(-10));
        TestEquality(Vector512.Create(10));
        TestEquality(Vector512.Create((sbyte)-10));
        TestEquality(Vector512.Create((ushort)10));
        TestEquality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector512.Create(0), Vector512.Create(0));
        TestEqualityUsingAnd(Vector512.Create(0.0f), Vector512.Create(0.0f));
        TestEqualityUsingAnd(Vector512.Create(-0.0f), Vector512.Create(-0.0f));
        TestEqualityUsingAnd(Vector512.Create(0.0), Vector512.Create(0.0));
        TestEqualityUsingAnd(Vector512.Create(-0.0), Vector512.Create(-0.0));

        TestEqualityUsingAndNot(Vector512.Create(0), Vector512.Create(0));
        TestEqualityUsingAndNot(Vector512.Create(0.0f), Vector512.Create(0.0f));
        TestEqualityUsingAndNot(Vector512.Create(-0.0f), Vector512.Create(-0.0f));
        TestEqualityUsingAndNot(Vector512.Create(0.0), Vector512.Create(0.0));
        TestEqualityUsingAndNot(Vector512.Create(-0.0), Vector512.Create(-0.0));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector64Inequality()
    {
        TestInequality(Vector64.Create(0));
        TestInequality(Vector64.Create(0.0f));
        TestInequality(Vector64.Create(-0.0f));
        TestInequality(Vector64.Create(0.0));
        TestInequality(Vector64.Create(-0.0));

        TestInequalityUsingReversedInputs(Vector64.Create(0));
        TestInequalityUsingReversedInputs(Vector64.Create(0.0f));
        TestInequalityUsingReversedInputs(Vector64.Create(-0.0f));
        TestInequalityUsingReversedInputs(Vector64.Create(0.0));
        TestInequalityUsingReversedInputs(Vector64.Create(-0.0));

        TestInequality(Vector64.Create(-10));
        TestInequality(Vector64.Create(10));
        TestInequality(Vector64.Create((sbyte)-10));
        TestInequality(Vector64.Create((ushort)10));
        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector64.Create(0), Vector64.Create(0));
        TestInequalityUsingAnd(Vector64.Create(0.0f), Vector64.Create(0.0f));
        TestInequalityUsingAnd(Vector64.Create(-0.0f), Vector64.Create(-0.0f));
        TestInequalityUsingAnd(Vector64.Create(0.0), Vector64.Create(0.0));
        TestInequalityUsingAnd(Vector64.Create(-0.0), Vector64.Create(-0.0));

        TestInequalityUsingAndNot(Vector64.Create(0), Vector64.Create(0));
        TestInequalityUsingAndNot(Vector64.Create(0.0f), Vector64.Create(0.0f));
        TestInequalityUsingAndNot(Vector64.Create(-0.0f), Vector64.Create(-0.0f));
        TestInequalityUsingAndNot(Vector64.Create(0.0), Vector64.Create(0.0));
        TestInequalityUsingAndNot(Vector64.Create(-0.0), Vector64.Create(-0.0));

        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));
        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, -1, 0));

        TestInequality(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestInequality(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));

        TestInequalityUsingReversedInputs(Vector64.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestInequalityUsingReversedInputs(Vector64.Create(0, 0, 0, -1, 0, 0, 0, -1));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector128Inequality()
    {
        TestInequality(Vector128.Create(0));
        TestInequality(Vector128.Create(0.0f));
        TestInequality(Vector128.Create(-0.0f));
        TestInequality(Vector128.Create(0.0));
        TestInequality(Vector128.Create(-0.0));

        TestInequalityUsingReversedInputs(Vector128.Create(0));
        TestInequalityUsingReversedInputs(Vector128.Create(0.0f));
        TestInequalityUsingReversedInputs(Vector128.Create(-0.0f));
        TestInequalityUsingReversedInputs(Vector128.Create(0.0));
        TestInequalityUsingReversedInputs(Vector128.Create(-0.0));

        TestInequality(Vector128.Create(-10));
        TestInequality(Vector128.Create(10));
        TestInequality(Vector128.Create((sbyte)-10));
        TestInequality(Vector128.Create((ushort)10));
        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector128.Create(0), Vector128.Create(0));
        TestInequalityUsingAnd(Vector128.Create(0.0f), Vector128.Create(0.0f));
        TestInequalityUsingAnd(Vector128.Create(-0.0f), Vector128.Create(-0.0f));
        TestInequalityUsingAnd(Vector128.Create(0.0), Vector128.Create(0.0));
        TestInequalityUsingAnd(Vector128.Create(-0.0), Vector128.Create(-0.0));

        TestInequalityUsingAndNot(Vector128.Create(0), Vector128.Create(0));
        TestInequalityUsingAndNot(Vector128.Create(0.0f), Vector128.Create(0.0f));
        TestInequalityUsingAndNot(Vector128.Create(-0.0f), Vector128.Create(-0.0f));
        TestInequalityUsingAndNot(Vector128.Create(0.0), Vector128.Create(0.0));
        TestInequalityUsingAndNot(Vector128.Create(-0.0), Vector128.Create(-0.0));

        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));
        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 1, 0));

        TestInequality(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestInequality(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));

        TestInequalityUsingReversedInputs(Vector128.Create(0, 0, 0, 1, 0, 0, 0, 1));
        TestInequalityUsingReversedInputs(Vector128.Create(0, 0, 0, -1, 0, 0, 0, -1));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector256Inequality()
    {
        TestInequality(Vector256.Create(0));
        TestInequality(Vector256.Create(0.0f));
        TestInequality(Vector256.Create(-0.0f));
        TestInequality(Vector256.Create(0.0));
        TestInequality(Vector256.Create(-0.0));

        TestInequalityUsingReversedInputs(Vector256.Create(0));
        TestInequalityUsingReversedInputs(Vector256.Create(0.0f));
        TestInequalityUsingReversedInputs(Vector256.Create(-0.0f));
        TestInequalityUsingReversedInputs(Vector256.Create(0.0));
        TestInequalityUsingReversedInputs(Vector256.Create(-0.0));

        TestInequality(Vector256.Create(-10));
        TestInequality(Vector256.Create(10));
        TestInequality(Vector256.Create((sbyte)-10));
        TestInequality(Vector256.Create((ushort)10));
        TestInequality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector256.Create(0), Vector256.Create(0));
        TestInequalityUsingAnd(Vector256.Create(0.0f), Vector256.Create(0.0f));
        TestInequalityUsingAnd(Vector256.Create(-0.0f), Vector256.Create(-0.0f));
        TestInequalityUsingAnd(Vector256.Create(0.0), Vector256.Create(0.0));
        TestInequalityUsingAnd(Vector256.Create(-0.0), Vector256.Create(-0.0));

        TestInequalityUsingAndNot(Vector256.Create(0), Vector256.Create(0));
        TestInequalityUsingAndNot(Vector256.Create(0.0f), Vector256.Create(0.0f));
        TestInequalityUsingAndNot(Vector256.Create(-0.0f), Vector256.Create(-0.0f));
        TestInequalityUsingAndNot(Vector256.Create(0.0), Vector256.Create(0.0));
        TestInequalityUsingAndNot(Vector256.Create(-0.0), Vector256.Create(-0.0));
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector512Inequality()
    {
        TestInequality(Vector512.Create(0));
        TestInequality(Vector512.Create(0.0f));
        TestInequality(Vector512.Create(-0.0f));
        TestInequality(Vector512.Create(0.0));
        TestInequality(Vector512.Create(-0.0));

        TestInequalityUsingReversedInputs(Vector512.Create(0));
        TestInequalityUsingReversedInputs(Vector512.Create(0.0f));
        TestInequalityUsingReversedInputs(Vector512.Create(-0.0f));
        TestInequalityUsingReversedInputs(Vector512.Create(0.0));
        TestInequalityUsingReversedInputs(Vector512.Create(-0.0));

        TestInequality(Vector512.Create(-10));
        TestInequality(Vector512.Create(10));
        TestInequality(Vector512.Create((sbyte)-10));
        TestInequality(Vector512.Create((ushort)10));
        TestInequality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector512.Create(0), Vector512.Create(0));
        TestInequalityUsingAnd(Vector512.Create(0.0f), Vector512.Create(0.0f));
        TestInequalityUsingAnd(Vector512.Create(-0.0f), Vector512.Create(-0.0f));
        TestInequalityUsingAnd(Vector512.Create(0.0), Vector512.Create(0.0));
        TestInequalityUsingAnd(Vector512.Create(-0.0), Vector512.Create(-0.0));

        TestInequalityUsingAndNot(Vector512.Create(0), Vector512.Create(0));
        TestInequalityUsingAndNot(Vector512.Create(0.0f), Vector512.Create(0.0f));
        TestInequalityUsingAndNot(Vector512.Create(-0.0f), Vector512.Create(-0.0f));
        TestInequalityUsingAndNot(Vector512.Create(0.0), Vector512.Create(0.0));
        TestInequalityUsingAndNot(Vector512.Create(-0.0), Vector512.Create(-0.0));
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
    static void TestEquality<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((v == Vector64<T>.Zero) ==
                   (v == Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEquality<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((v == Vector128<T>.Zero) ==
                   (v == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEquality<T>(Vector256<T> v) where T : unmanaged =>
        AssertTrue((v == Vector256<T>.Zero) ==
                   (v == Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEquality<T>(Vector512<T> v) where T : unmanaged =>
        AssertTrue((v == Vector512<T>.Zero) ==
                   (v == Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAnd<T>(Vector64<T> v1, Vector64<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) == Vector64<T>.Zero) ==
                   ((v1 & v2) == Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAnd<T>(Vector128<T> v1,Vector128<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) == Vector128<T>.Zero) ==
                   ((v1 & v2) == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAnd<T>(Vector256<T> v1, Vector256<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) == Vector256<T>.Zero) ==
                   ((v1 & v2) == Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAnd<T>(Vector512<T> v1, Vector512<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) == Vector512<T>.Zero) ==
                   ((v1 & v2) == Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAndNot<T>(Vector64<T> v1, Vector64<T> v2) where T : unmanaged =>
        AssertTrue((Vector64.AndNot(v1, v2) == Vector64<T>.Zero) ==
                   (Vector64.AndNot(v1, v2) == Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAndNot<T>(Vector128<T> v1,Vector128<T> v2) where T : unmanaged =>
        AssertTrue((Vector128.AndNot(v1, v2) == Vector128<T>.Zero) ==
                   (Vector128.AndNot(v1, v2) == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAndNot<T>(Vector256<T> v1, Vector256<T> v2) where T : unmanaged =>
        AssertTrue((Vector256.AndNot(v1, v2) == Vector256<T>.Zero) ==
                   (Vector256.AndNot(v1, v2) == Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingAndNot<T>(Vector512<T> v1, Vector512<T> v2) where T : unmanaged =>
        AssertTrue((Vector512.AndNot(v1, v2) == Vector512<T>.Zero) ==
                   (Vector512.AndNot(v1, v2) == Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingReversedInputs<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((Vector64<T>.Zero == v) ==
                   (v == Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingReversedInputs<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((Vector128<T>.Zero == v) ==
                   (v == Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingReversedInputs<T>(Vector256<T> v) where T : unmanaged =>
        AssertTrue((Vector256<T>.Zero == v) ==
                   (v == Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestEqualityUsingReversedInputs<T>(Vector512<T> v) where T : unmanaged =>
        AssertTrue((Vector512<T>.Zero == v) ==
                   (v == Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequality<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((v != Vector64<T>.Zero) ==
                   (v != Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequality<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((v != Vector128<T>.Zero) ==
                   (v != Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequality<T>(Vector256<T> v) where T : unmanaged =>
        AssertTrue((v != Vector256<T>.Zero) ==
                   (v != Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequality<T>(Vector512<T> v) where T : unmanaged =>
        AssertTrue((v != Vector512<T>.Zero) ==
                   (v != Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAnd<T>(Vector64<T> v1, Vector64<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) != Vector64<T>.Zero) ==
                   ((v1 & v2) != Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAnd<T>(Vector128<T> v1,Vector128<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) != Vector128<T>.Zero) ==
                   ((v1 & v2) != Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAnd<T>(Vector256<T> v1, Vector256<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) != Vector256<T>.Zero) ==
                   ((v1 & v2) != Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAnd<T>(Vector512<T> v1, Vector512<T> v2) where T : unmanaged =>
        AssertTrue(((v1 & v2) != Vector512<T>.Zero) ==
                   ((v1 & v2) != Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAndNot<T>(Vector64<T> v1, Vector64<T> v2) where T : unmanaged =>
        AssertTrue((Vector64.AndNot(v1, v2) != Vector64<T>.Zero) ==
                   (Vector64.AndNot(v1, v2) != Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAndNot<T>(Vector128<T> v1,Vector128<T> v2) where T : unmanaged =>
        AssertTrue((Vector128.AndNot(v1, v2) != Vector128<T>.Zero) ==
                   (Vector128.AndNot(v1, v2) != Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAndNot<T>(Vector256<T> v1, Vector256<T> v2) where T : unmanaged =>
        AssertTrue((Vector256.AndNot(v1, v2) != Vector256<T>.Zero) ==
                   (Vector256.AndNot(v1, v2) != Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingAndNot<T>(Vector512<T> v1, Vector512<T> v2) where T : unmanaged =>
        AssertTrue((Vector512.AndNot(v1, v2) != Vector512<T>.Zero) ==
                   (Vector512.AndNot(v1, v2) != Vector512.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingReversedInputs<T>(Vector64<T> v) where T : unmanaged =>
        AssertTrue((Vector64<T>.Zero != v) ==
                   (v != Vector64.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingReversedInputs<T>(Vector128<T> v) where T : unmanaged =>
        AssertTrue((Vector128<T>.Zero != v) ==
                   (v != Vector128.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingReversedInputs<T>(Vector256<T> v) where T : unmanaged =>
        AssertTrue((Vector256<T>.Zero != v) ==
                   (v != Vector256.Create(ToVar(default(T)))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestInequalityUsingReversedInputs<T>(Vector512<T> v) where T : unmanaged =>
        AssertTrue((Vector512<T>.Zero != v) ==
                   (v != Vector512.Create(ToVar(default(T)))));
}
