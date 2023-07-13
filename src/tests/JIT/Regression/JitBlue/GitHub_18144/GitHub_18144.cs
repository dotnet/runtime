// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class GitHub_18144
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void dummy(Vector256<byte> v1, Vector256<byte> v2, Vector256<byte> v3, Vector256<byte> v4,
                      Vector256<byte> v5, Vector256<byte> v6, Vector256<byte> v7, Vector256<byte> v8)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoThis()
    {
        var vA = Vector256.Create((byte)0xa);
        var vB = Vector256.Create((byte)0xb);
        var vC = Vector256.Create((byte)0xc);
        var vD = Vector256.Create((byte)0xd);
        var vE = Vector256.Create((byte)0xe);
        var vF = Vector256.Create((byte)0xf);
        var vG = Vector256.Create((byte)0x8);
        var vH = Vector256.Create((byte)0x9);
        dummy(vA, vB, vC, vD, vE, vF, vG, vH);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoThat() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void dummy128(Vector128<byte> v1, Vector128<byte> v2, Vector128<byte> v3, Vector128<byte> v4,
                         Vector128<byte> v5, Vector128<byte> v6, Vector128<byte> v7, Vector128<byte> v8)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoThis128()
    {
        var vA = Vector128.Create((byte)0xa);
        var vB = Vector128.Create((byte)0xb);
        var vC = Vector128.Create((byte)0xc);
        var vD = Vector128.Create((byte)0xd);
        var vE = Vector128.Create((byte)0xe);
        var vF = Vector128.Create((byte)0xf);
        var vG = Vector128.Create((byte)0x8);
        var vH = Vector128.Create((byte)0x9);
        dummy128(vA, vB, vC, vD, vE, vF, vG, vH);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;

        var xA = Vector256<byte>.Zero;
        var xB = Vector256<byte>.Zero;
        var xC = Vector256<byte>.Zero;
        var xD = Vector256<byte>.Zero;
        var xE = Vector256<byte>.Zero;
        var xF = Vector256<byte>.Zero;
        var xG = Vector256<byte>.Zero;
        var xH = Vector256<byte>.Zero;

        DoThis();
        DoThat();

        Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7}", xA, xB, xC, xD, xE, xF, xG, xH);
        if (!xA.Equals(Vector256<byte>.Zero) || !xB.Equals(Vector256<byte>.Zero) || !xC.Equals(Vector256<byte>.Zero) || !xD.Equals(Vector256<byte>.Zero) ||
            !xE.Equals(Vector256<byte>.Zero) || !xF.Equals(Vector256<byte>.Zero) || !xG.Equals(Vector256<byte>.Zero) || !xH.Equals(Vector256<byte>.Zero))
        {
            returnVal = -1;
        }

        var vA = Vector128<byte>.Zero;
        var vB = Vector128<byte>.Zero;
        var vC = Vector128<byte>.Zero;
        var vD = Vector128<byte>.Zero;
        var vE = Vector128<byte>.Zero;
        var vF = Vector128<byte>.Zero;
        var vG = Vector128<byte>.Zero;
        var vH = Vector128<byte>.Zero;

        DoThis128();
        DoThat();

        Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7}", vA, vB, vC, vD, vE, vF, vG, vH);
        if (!vA.Equals(Vector128<byte>.Zero) || !vB.Equals(Vector128<byte>.Zero) || !vC.Equals(Vector128<byte>.Zero) || !vD.Equals(Vector128<byte>.Zero) ||
            !vE.Equals(Vector128<byte>.Zero) || !vF.Equals(Vector128<byte>.Zero) || !vG.Equals(Vector128<byte>.Zero) || !vH.Equals(Vector128<byte>.Zero))
        {
            returnVal = -1;
        }

        return returnVal;
    }
}
