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
        TestEquality(Vector64<float>.Zero);
        TestEquality(Vector64<float>.NegativeZero);
        TestEquality(Vector64<double>.Zero);
        TestEquality(Vector64<double>.NegativeZero);

        TestEqualityUsingReversedInputs(Vector64.Create(0));
        TestEqualityUsingReversedInputs(Vector64<float>.Zero);
        TestEqualityUsingReversedInputs(Vector64<float>.NegativeZero);
        TestEqualityUsingReversedInputs(Vector64<double>.Zero);
        TestEqualityUsingReversedInputs(Vector64<double>.NegativeZero);

        TestEquality(Vector64.Create(-10));
        TestEquality(Vector64.Create(10));
        TestEquality(Vector64.Create((sbyte)-10));
        TestEquality(Vector64.Create((ushort)10));
        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector64.Create(0), Vector64.Create(0));
        TestEqualityUsingAnd(Vector64<float>.Zero, Vector64<float>.Zero);
        TestEqualityUsingAnd(Vector64<float>.NegativeZero, Vector64<float>.NegativeZero);
        TestEqualityUsingAnd(Vector64<double>.Zero, Vector64<double>.Zero);
        TestEqualityUsingAnd(Vector64<double>.NegativeZero, Vector64<double>.NegativeZero);

        TestEqualityUsingAndNot(Vector64.Create(0), Vector64.Create(0));
        TestEqualityUsingAndNot(Vector64<float>.Zero, Vector64<float>.Zero);
        TestEqualityUsingAndNot(Vector64<float>.NegativeZero, Vector64<float>.NegativeZero);
        TestEqualityUsingAndNot(Vector64<double>.Zero, Vector64<double>.Zero);
        TestEqualityUsingAndNot(Vector64<double>.NegativeZero, Vector64<double>.NegativeZero);

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
        TestEquality(Vector128<float>.Zero);
        TestEquality(Vector128<float>.NegativeZero);
        TestEquality(Vector128<double>.Zero);
        TestEquality(Vector128<double>.NegativeZero);

        TestEqualityUsingReversedInputs(Vector128.Create(0));
        TestEqualityUsingReversedInputs(Vector128<float>.Zero);
        TestEqualityUsingReversedInputs(Vector128<float>.NegativeZero);
        TestEqualityUsingReversedInputs(Vector128<double>.Zero);
        TestEqualityUsingReversedInputs(Vector128<double>.NegativeZero);

        TestEquality(Vector128.Create(-10));
        TestEquality(Vector128.Create(10));
        TestEquality(Vector128.Create((sbyte)-10));
        TestEquality(Vector128.Create((ushort)10));
        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector128.Create(0), Vector128.Create(0));
        TestEqualityUsingAnd(Vector128<float>.Zero, Vector128<float>.Zero);
        TestEqualityUsingAnd(Vector128<float>.NegativeZero, Vector128<float>.NegativeZero);
        TestEqualityUsingAnd(Vector128<double>.Zero, Vector128<double>.Zero);
        TestEqualityUsingAnd(Vector128<double>.NegativeZero, Vector128<double>.NegativeZero);

        TestEqualityUsingAndNot(Vector128.Create(0), Vector128.Create(0));
        TestEqualityUsingAndNot(Vector128<float>.Zero, Vector128<float>.Zero);
        TestEqualityUsingAndNot(Vector128<float>.NegativeZero, Vector128<float>.NegativeZero);
        TestEqualityUsingAndNot(Vector128<double>.Zero, Vector128<double>.Zero);
        TestEqualityUsingAndNot(Vector128<double>.NegativeZero, Vector128<double>.NegativeZero);

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
        TestEquality(Vector256<float>.Zero);
        TestEquality(Vector256<float>.NegativeZero);
        TestEquality(Vector256<double>.Zero);
        TestEquality(Vector256<double>.NegativeZero);

        TestEqualityUsingReversedInputs(Vector256.Create(0));
        TestEqualityUsingReversedInputs(Vector256<float>.Zero);
        TestEqualityUsingReversedInputs(Vector256<float>.NegativeZero);
        TestEqualityUsingReversedInputs(Vector256<double>.Zero);
        TestEqualityUsingReversedInputs(Vector256<double>.NegativeZero);

        TestEquality(Vector256.Create(-10));
        TestEquality(Vector256.Create(10));
        TestEquality(Vector256.Create((sbyte)-10));
        TestEquality(Vector256.Create((ushort)10));
        TestEquality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector256.Create(0), Vector256.Create(0));
        TestEqualityUsingAnd(Vector256<float>.Zero, Vector256<float>.Zero);
        TestEqualityUsingAnd(Vector256<float>.NegativeZero, Vector256<float>.NegativeZero);
        TestEqualityUsingAnd(Vector256<double>.Zero, Vector256<double>.Zero);
        TestEqualityUsingAnd(Vector256<double>.NegativeZero, Vector256<double>.NegativeZero);

        TestEqualityUsingAndNot(Vector256.Create(0), Vector256.Create(0));
        TestEqualityUsingAndNot(Vector256<float>.Zero, Vector256<float>.Zero);
        TestEqualityUsingAndNot(Vector256<float>.NegativeZero, Vector256<float>.NegativeZero);
        TestEqualityUsingAndNot(Vector256<double>.Zero, Vector256<double>.Zero);
        TestEqualityUsingAndNot(Vector256<double>.NegativeZero, Vector256<double>.NegativeZero);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector512Equality()
    {
        TestEquality(Vector512.Create(0));
        TestEquality(Vector512<float>.Zero);
        TestEquality(Vector512<float>.NegativeZero);
        TestEquality(Vector512<double>.Zero);
        TestEquality(Vector512<double>.NegativeZero);

        TestEqualityUsingReversedInputs(Vector512.Create(0));
        TestEqualityUsingReversedInputs(Vector512<float>.Zero);
        TestEqualityUsingReversedInputs(Vector512<float>.NegativeZero);
        TestEqualityUsingReversedInputs(Vector512<double>.Zero);
        TestEqualityUsingReversedInputs(Vector512<double>.NegativeZero);

        TestEquality(Vector512.Create(-10));
        TestEquality(Vector512.Create(10));
        TestEquality(Vector512.Create((sbyte)-10));
        TestEquality(Vector512.Create((ushort)10));
        TestEquality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestEquality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestEqualityUsingAnd(Vector512.Create(0), Vector512.Create(0));
        TestEqualityUsingAnd(Vector512<float>.Zero, Vector512<float>.Zero);
        TestEqualityUsingAnd(Vector512<float>.NegativeZero, Vector512<float>.NegativeZero);
        TestEqualityUsingAnd(Vector512<double>.Zero, Vector512<double>.Zero);
        TestEqualityUsingAnd(Vector512<double>.NegativeZero, Vector512<double>.NegativeZero);

        TestEqualityUsingAndNot(Vector512.Create(0), Vector512.Create(0));
        TestEqualityUsingAndNot(Vector512<float>.Zero, Vector512<float>.Zero);
        TestEqualityUsingAndNot(Vector512<float>.NegativeZero, Vector512<float>.NegativeZero);
        TestEqualityUsingAndNot(Vector512<double>.Zero, Vector512<double>.Zero);
        TestEqualityUsingAndNot(Vector512<double>.NegativeZero, Vector512<double>.NegativeZero);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector64Inequality()
    {
        TestInequality(Vector64.Create(0));
        TestInequality(Vector64<float>.Zero);
        TestInequality(Vector64<float>.NegativeZero);
        TestInequality(Vector64<double>.Zero);
        TestInequality(Vector64<double>.NegativeZero);

        TestInequalityUsingReversedInputs(Vector64.Create(0));
        TestInequalityUsingReversedInputs(Vector64<float>.Zero);
        TestInequalityUsingReversedInputs(Vector64<float>.NegativeZero);
        TestInequalityUsingReversedInputs(Vector64<double>.Zero);
        TestInequalityUsingReversedInputs(Vector64<double>.NegativeZero);

        TestInequality(Vector64.Create(-10));
        TestInequality(Vector64.Create(10));
        TestInequality(Vector64.Create((sbyte)-10));
        TestInequality(Vector64.Create((ushort)10));
        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector64.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector64.Create(0), Vector64.Create(0));
        TestInequalityUsingAnd(Vector64<float>.Zero, Vector64<float>.Zero);
        TestInequalityUsingAnd(Vector64<float>.NegativeZero, Vector64<float>.NegativeZero);
        TestInequalityUsingAnd(Vector64<double>.Zero, Vector64<double>.Zero);
        TestInequalityUsingAnd(Vector64<double>.NegativeZero, Vector64<double>.NegativeZero);

        TestInequalityUsingAndNot(Vector64.Create(0), Vector64.Create(0));
        TestInequalityUsingAndNot(Vector64<float>.Zero, Vector64<float>.Zero);
        TestInequalityUsingAndNot(Vector64<float>.NegativeZero, Vector64<float>.NegativeZero);
        TestInequalityUsingAndNot(Vector64<double>.Zero, Vector64<double>.Zero);
        TestInequalityUsingAndNot(Vector64<double>.NegativeZero, Vector64<double>.NegativeZero);

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
        TestInequality(Vector128<float>.Zero);
        TestInequality(Vector128<float>.NegativeZero);
        TestInequality(Vector128<double>.Zero);
        TestInequality(Vector128<double>.NegativeZero);

        TestInequalityUsingReversedInputs(Vector128.Create(0));
        TestInequalityUsingReversedInputs(Vector128<float>.Zero);
        TestInequalityUsingReversedInputs(Vector128<float>.NegativeZero);
        TestInequalityUsingReversedInputs(Vector128<double>.Zero);
        TestInequalityUsingReversedInputs(Vector128<double>.NegativeZero);

        TestInequality(Vector128.Create(-10));
        TestInequality(Vector128.Create(10));
        TestInequality(Vector128.Create((sbyte)-10));
        TestInequality(Vector128.Create((ushort)10));
        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector128.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector128.Create(0), Vector128.Create(0));
        TestInequalityUsingAnd(Vector128<float>.Zero, Vector128<float>.Zero);
        TestInequalityUsingAnd(Vector128<float>.NegativeZero, Vector128<float>.NegativeZero);
        TestInequalityUsingAnd(Vector128<double>.Zero, Vector128<double>.Zero);
        TestInequalityUsingAnd(Vector128<double>.NegativeZero, Vector128<double>.NegativeZero);

        TestInequalityUsingAndNot(Vector128.Create(0), Vector128.Create(0));
        TestInequalityUsingAndNot(Vector128<float>.Zero, Vector128<float>.Zero);
        TestInequalityUsingAndNot(Vector128<float>.NegativeZero, Vector128<float>.NegativeZero);
        TestInequalityUsingAndNot(Vector128<double>.Zero, Vector128<double>.Zero);
        TestInequalityUsingAndNot(Vector128<double>.NegativeZero, Vector128<double>.NegativeZero);

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
        TestInequality(Vector256<float>.Zero);
        TestInequality(Vector256<float>.NegativeZero);
        TestInequality(Vector256<double>.Zero);
        TestInequality(Vector256<double>.NegativeZero);

        TestInequalityUsingReversedInputs(Vector256.Create(0));
        TestInequalityUsingReversedInputs(Vector256<float>.Zero);
        TestInequalityUsingReversedInputs(Vector256<float>.NegativeZero);
        TestInequalityUsingReversedInputs(Vector256<double>.Zero);
        TestInequalityUsingReversedInputs(Vector256<double>.NegativeZero);

        TestInequality(Vector256.Create(-10));
        TestInequality(Vector256.Create(10));
        TestInequality(Vector256.Create((sbyte)-10));
        TestInequality(Vector256.Create((ushort)10));
        TestInequality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector256.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector256.Create(0), Vector256.Create(0));
        TestInequalityUsingAnd(Vector256<float>.Zero, Vector256<float>.Zero);
        TestInequalityUsingAnd(Vector256<float>.NegativeZero, Vector256<float>.NegativeZero);
        TestInequalityUsingAnd(Vector256<double>.Zero, Vector256<double>.Zero);
        TestInequalityUsingAnd(Vector256<double>.NegativeZero, Vector256<double>.NegativeZero);

        TestInequalityUsingAndNot(Vector256.Create(0), Vector256.Create(0));
        TestInequalityUsingAndNot(Vector256<float>.Zero, Vector256<float>.Zero);
        TestInequalityUsingAndNot(Vector256<float>.NegativeZero, Vector256<float>.NegativeZero);
        TestInequalityUsingAndNot(Vector256<double>.Zero, Vector256<double>.Zero);
        TestInequalityUsingAndNot(Vector256<double>.NegativeZero, Vector256<double>.NegativeZero);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/pull/65632#issuecomment-1046294324", TestRuntimes.Mono)]
    [Fact]
    public static void TestVector512Inequality()
    {
        TestInequality(Vector512.Create(0));
        TestInequality(Vector512<float>.Zero);
        TestInequality(Vector512<float>.NegativeZero);
        TestInequality(Vector512<double>.Zero);
        TestInequality(Vector512<double>.NegativeZero);

        TestInequalityUsingReversedInputs(Vector512.Create(0));
        TestInequalityUsingReversedInputs(Vector512<float>.Zero);
        TestInequalityUsingReversedInputs(Vector512<float>.NegativeZero);
        TestInequalityUsingReversedInputs(Vector512<double>.Zero);
        TestInequalityUsingReversedInputs(Vector512<double>.NegativeZero);

        TestInequality(Vector512.Create(-10));
        TestInequality(Vector512.Create(10));
        TestInequality(Vector512.Create((sbyte)-10));
        TestInequality(Vector512.Create((ushort)10));
        TestInequality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, 1));
        TestInequality(Vector512.Create(0, 0, 0, 0, 0, 0, 0, -1));

        TestInequalityUsingAnd(Vector512.Create(0), Vector512.Create(0));
        TestInequalityUsingAnd(Vector512<float>.Zero, Vector512<float>.Zero);
        TestInequalityUsingAnd(Vector512<float>.NegativeZero, Vector512<float>.NegativeZero);
        TestInequalityUsingAnd(Vector512<double>.Zero, Vector512<double>.Zero);
        TestInequalityUsingAnd(Vector512<double>.NegativeZero, Vector512<double>.NegativeZero);

        TestInequalityUsingAndNot(Vector512.Create(0), Vector512.Create(0));
        TestInequalityUsingAndNot(Vector512<float>.Zero, Vector512<float>.Zero);
        TestInequalityUsingAndNot(Vector512<float>.NegativeZero, Vector512<float>.NegativeZero);
        TestInequalityUsingAndNot(Vector512<double>.Zero, Vector512<double>.Zero);
        TestInequalityUsingAndNot(Vector512<double>.NegativeZero, Vector512<double>.NegativeZero);
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
