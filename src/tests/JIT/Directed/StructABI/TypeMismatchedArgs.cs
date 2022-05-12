// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public unsafe class TypeMismatchedArgs
{
    private static readonly HfaUnion s_hfaDblFlt = new HfaUnion { DblHfa = { FirstDblValue = 1.0, SecondDblValue = 2.0 } };
    private static readonly HfaDblLngUnion s_dblLngHfa = new HfaDblLngUnion { DblLng = { FirstLngValue = 10, SecondLngValue = 20 } };

    public static int Main()
    {
        if (ProblemWithHfasMismatch(s_hfaDblFlt.FltHfa))
        {
            return 101;
        }

        if (ProblemWithHfaIntMismatch(s_dblLngHfa.DblLng))
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithHfaIntMismatch(DblLngStruct dblLng)
    {
        var result = CallForHfaDblStruct(*(HfaDblStruct*)&dblLng);

        return result != s_dblLngHfa.DblHfa.FirstDblValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithHfasMismatch(HfaFltStruct fltHfa)
    {
        var result = CallForHfaDblStruct(*(HfaDblStruct*)&fltHfa);

        return result != s_hfaDblFlt.DblHfa.FirstDblValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CallForHfaDblStruct(HfaDblStruct value) => value.FirstDblValue;
}

[StructLayout(LayoutKind.Explicit)]
struct HfaDblLngUnion
{
    [FieldOffset(0)]
    public HfaDblStruct DblHfa;
    [FieldOffset(0)]
    public DblLngStruct DblLng;
}

[StructLayout(LayoutKind.Explicit)]
struct HfaUnion
{
    [FieldOffset(0)]
    public HfaDblStruct DblHfa;
    [FieldOffset(0)]
    public HfaFltStruct FltHfa;
}

struct DblLngStruct
{
    public long FirstLngValue;
    public long SecondLngValue;
}

struct HfaDblStruct
{
    public double FirstDblValue;
    public double SecondDblValue;
}

struct HfaFltStruct
{
    public float FirstFltValue;
    public float SecondFltValue;
    public float ThirdFltValue;
    public float FourthFltValue;
}
