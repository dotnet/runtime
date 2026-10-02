// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

public class WasmR2RStructAlignment
{
    // The leading long leaves the aggregate at an offset where 8- and 16-byte alignment differ.
    // The trailing byref makes any disagreement shift a managed pointer as well as the aggregate.
    private const long Prefix = 0x1122334455667788;
    private const int InitialTrailer = 0x12345678;
    private const int UpdatedTrailer = 0x76543210;
    private static readonly object s_reference = new();

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(UpdatedTrailer, R2RAutoLayoutCaller());
        Assert.Equal(UpdatedTrailer, InterpretedAutoLayoutCaller());
        Assert.Equal(UpdatedTrailer, R2RExplicitLayoutCaller());
        Assert.Equal(UpdatedTrailer, InterpretedExplicitLayoutCaller());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int R2RAutoLayoutCaller()
    {
        int trailer = InitialTrailer;
        ValueTuple<Vector128<float>, Vector128<float>> value =
            (Vector128.Create(1.0f), Vector128.Create(2.0f));

        int result = InterpretedAutoLayoutCallee(Prefix, value, ref trailer);

        Assert.Equal(UpdatedTrailer, trailer);
        return result;
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InterpretedAutoLayoutCaller()
    {
        int trailer = InitialTrailer;
        ValueTuple<Vector128<float>, Vector128<float>> value =
            (Vector128.Create(1.0f), Vector128.Create(2.0f));

        int result = R2RAutoLayoutCallee(Prefix, value, ref trailer);

        Assert.Equal(UpdatedTrailer, trailer);
        return result;
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InterpretedAutoLayoutCallee(
        long prefix,
        ValueTuple<Vector128<float>, Vector128<float>> value,
        ref int trailer)
    {
        Assert.Equal(Prefix, prefix);
        Assert.Equal(Vector128.Create(1.0f), value.Item1);
        Assert.Equal(Vector128.Create(2.0f), value.Item2);
        Assert.Equal(InitialTrailer, trailer);
        trailer = UpdatedTrailer;
        return trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int R2RAutoLayoutCallee(
        long prefix,
        ValueTuple<Vector128<float>, Vector128<float>> value,
        ref int trailer)
    {
        Assert.Equal(Prefix, prefix);
        Assert.Equal(Vector128.Create(1.0f), value.Item1);
        Assert.Equal(Vector128.Create(2.0f), value.Item2);
        Assert.Equal(InitialTrailer, trailer);
        trailer = UpdatedTrailer;
        return trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int R2RExplicitLayoutCaller()
    {
        int trailer = InitialTrailer;
        ExplicitLayout value = new()
        {
            Vector = Vector128.Create(3.0f),
            Reference = s_reference,
        };

        int result = InterpretedExplicitLayoutCallee(Prefix, value, ref trailer);

        Assert.Equal(UpdatedTrailer, trailer);
        return result;
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InterpretedExplicitLayoutCaller()
    {
        int trailer = InitialTrailer;
        ExplicitLayout value = new()
        {
            Vector = Vector128.Create(3.0f),
            Reference = s_reference,
        };

        int result = R2RExplicitLayoutCallee(Prefix, value, ref trailer);

        Assert.Equal(UpdatedTrailer, trailer);
        return result;
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InterpretedExplicitLayoutCallee(
        long prefix,
        ExplicitLayout value,
        ref int trailer)
    {
        Assert.Equal(Prefix, prefix);
        Assert.Equal(Vector128.Create(3.0f), value.Vector);
        Assert.Same(s_reference, value.Reference);
        Assert.Equal(InitialTrailer, trailer);
        trailer = UpdatedTrailer;
        return trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int R2RExplicitLayoutCallee(
        long prefix,
        ExplicitLayout value,
        ref int trailer)
    {
        Assert.Equal(Prefix, prefix);
        Assert.Equal(Vector128.Create(3.0f), value.Vector);
        Assert.Same(s_reference, value.Reference);
        Assert.Equal(InitialTrailer, trailer);
        trailer = UpdatedTrailer;
        return trailer;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct ExplicitLayout
    {
        [FieldOffset(0)]
        public Vector128<float> Vector;

        [FieldOffset(16)]
        public object Reference;
    }
}
