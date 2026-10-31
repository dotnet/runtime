// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135822
{
    private struct S
    {
        public int A;
        public int B;
    }

    private ref struct R
    {
        public ref S Ref;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public static void TestEntryPoint(bool nullRef, bool sideEffects)
    {
        S value = new S { A = 1, B = 2 };
        R r = new R { Ref = ref value };
        if (nullRef)
        {
            r = default;
        }

        int evaluations = 0;
        bool threw = false;
        try
        {
            if (sideEffects)
            {
                CheckWithSideEffects(r, ref evaluations);
            }
            else
            {
                Check(r);
            }
        }
        catch (NullReferenceException)
        {
            threw = true;
        }

        Assert.Equal(nullRef, threw);
        Assert.Equal(sideEffects ? (nullRef ? 1 : 12) : 0, evaluations);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Check(R r) => ArgumentNullException.ThrowIfNull(r.Ref, nameof(r));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckWithSideEffects(R r, ref int evaluations) =>
        ArgumentNullException.ThrowIfNull(GetRef(r, ref evaluations), GetName(ref evaluations));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref S GetRef(R r, ref int evaluations)
    {
        evaluations++;
        return ref r.Ref;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetName(ref int evaluations)
    {
        evaluations = evaluations * 10 + 2;
        return nameof(evaluations);
    }
}
