// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class ZeroOffsetFieldSeqs
{
    private static UnionStruct s_union;

    [Fact]
    public static int TestEntryPoint()
    {
        if (ProblemWithArrayUnions(new UnionStruct[] { default }))
        {
            return 101;
        }

        if (ProblemWithStaticUnions())
        {
            return 102;
        }

        if (AnotherProblemWithArrayUnions(new UnionStruct[] { default }))
        {
            return 103;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithArrayUnions(UnionStruct[] a)
    {
        if (a[0].UnionOne.UnionOneFldTwo == 0)
        {
            a[0].UnionTwo.UnionTwoFldTwo = 1;
            if (a[0].UnionOne.UnionOneFldTwo == 0)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStaticUnions()
    {
        if (s_union.UnionOne.UnionOneFldTwo == 0)
        {
            s_union.UnionTwo.UnionTwoFldTwo = 1;
            if (s_union.UnionOne.UnionOneFldTwo == 0)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AnotherProblemWithArrayUnions(UnionStruct[] a)
    {
        ref var p1 = ref a[0];
        ref var p1a = ref Unsafe.Add(ref p1, 0).UnionOne;
        ref var p1b = ref Unsafe.Add(ref p1, 0).UnionTwo;

        if (p1a.UnionOneFldTwo == 0)
        {
            p1b.UnionTwoFldTwo = 1;
            if (p1a.UnionOneFldTwo == 0)
            {
                return true;
            }
        }

        return false;
    }
}

[StructLayout(LayoutKind.Explicit)]
struct UnionStruct
{
    [FieldOffset(0)]
    public UnionPartOne UnionOne;
    [FieldOffset(0)]
    public UnionPartTwo UnionTwo;
}

struct UnionPartOne
{
    public long UnionOneFldOne;
    public long UnionOneFldTwo;
}

struct UnionPartTwo
{
    public long UnionTwoFldOne;
    public long UnionTwoFldTwo;
}

