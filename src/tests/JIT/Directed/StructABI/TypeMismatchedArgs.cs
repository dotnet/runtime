// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public unsafe class TypeMismatchedArgs
{
    private static readonly HfaUnion s_hfaDblFlt = new HfaUnion { DblHfa = { FirstDblValue = 1.0, SecondDblValue = 2.0 } };
    private static readonly HfaDblLngUnion s_dblLngHfa = new HfaDblLngUnion { DblLng = { FirstLngValue = 10, SecondLngValue = 20 } };
    private static readonly FourDblLngUnion s_fourDblLngHfa = new FourDblLngUnion { Lngs = { LongOne = 30 } };
    private static readonly Vtor128Union s_vtor128 = new Vtor128Union { Vtor4 = new Vector4(4, 3, 2, 1) };

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

        if (ProblemWithSplitStructBlkMismatch())
        {
            return 103;
        }

        if (ProblemWithSplitStructHfaMismatch(s_fourDblLngHfa.Hfa))
        {
            return 104;
        }

        if (ProblemWithVectorCallArg())
        {
            return 105;
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
    private static bool ProblemWithSplitStructBlkMismatch()
    {
        var blk = stackalloc byte[sizeof(StructWithFourLongs)];
        var result = CallForSplitStructWithFourLongs(1, 1, *(StructWithFourLongs*)blk);

        // The stackalloc should have been zeroed-out.
        return result != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithSplitStructHfaMismatch(FourDoublesHfaStruct fourDblHfa)
    {
        var result = CallForSplitStructWithFourLongs(1, 1, *(StructWithFourLongs*)&fourDblHfa);

        return result != s_fourDblLngHfa.Lngs.LongOne;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithVectorCallArg()
    {
        var result = CallForVector4(GetVector128().AsVector4());

        return result != s_vtor128.Vtor4.X;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float CallForVector4(Vector4 value) => value.X;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> GetVector128() => s_vtor128.Vtor128;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CallForHfaDblStruct(HfaDblStruct value) => value.FirstDblValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CallForSplitStructWithFourLongs(int arg0, int arg1, StructWithFourLongs splitArg) => splitArg.LongOne;
}

[StructLayout(LayoutKind.Explicit)]
struct Vtor128Union
{
    [FieldOffset(0)]
    public Vector4 Vtor4;
    [FieldOffset(0)]
    public Vector128<float> Vtor128;
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

[StructLayout(LayoutKind.Explicit)]
struct FourDblLngUnion
{
    [FieldOffset(0)]
    public FourDoublesHfaStruct Hfa;
    [FieldOffset(0)]
    public StructWithFourLongs Lngs;
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

struct StructWithFourLongs
{
    public long LongOne;
    public long LongTwo;
    public long LongThree;
    public long LongFour;
}

struct FourDoublesHfaStruct
{
    public double FirstDblValue;
    public double SecondDblValue;
    public double ThirdDblValue;
    public double FourthDblValue;
}
