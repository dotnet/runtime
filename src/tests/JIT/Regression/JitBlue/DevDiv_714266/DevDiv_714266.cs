// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Based on DevDiv_714266, the issue reporoduced with JitStressRegs=0x1.
// `minRegCandidateCount` for `RefTypeUpperVectorSaveDef` did not count one temporary register
// that it used for the save. So if we had a call that did not require any registers (no defs/uses)
// then we set `minRegCandidateCount = 0`for `RefTypeUpperVectorSaveDef` `refPosition` 
// and was not able to find a register for do the saving.

public class DevDiv_714266
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CallWithoutUsesAndDefs()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void MethodWithManyLiveVectors()
    {
        Vector<float> v = new Vector<float>();

        Vector<float> v0 = -v;
        Vector<float> v1 = -v;
        Vector<float> v2 = -v;
        Vector<float> v3 = -v;
        Vector<float> v4 = -v;
        Vector<float> v5 = -v;
        Vector<float> v6 = -v;
        Vector<float> v7 = -v;
        Vector<float> v8 = -v;
        Vector<float> v9 = -v;

        CallWithoutUsesAndDefs();

        GC.KeepAlive(new object[10]
        {
            v1,
            v2,
            v3,
            v4,
            v5,
            v6,
            v7,
            v8,
            v9,
            v0
        });
    }

    [Fact]
    public static int TestEntryPoint()
    {
        MethodWithManyLiveVectors();
        return 100;
    }

}
