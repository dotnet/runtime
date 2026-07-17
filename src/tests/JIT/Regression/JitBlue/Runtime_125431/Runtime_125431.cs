// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_125431
{
    public static byte[] s_27;
    public static Vector256<byte>[,] s_53;

    private static bool s_op1Evaluated;

    [ConditionalFact(typeof(Avx2), nameof(Avx2.IsSupported))]
    public static void TestEntryPoint()
    {
        // The mask is a constant zero, so BlendVariable selects its first operand. Folding the
        // intrinsic must not reorder the second operand's side effects (the null s_53 load) ahead
        // of the first operand's side effects (the M43 call), so M43 must run before the null load
        // throws.
        s_op1Evaluated = false;
        Assert.Throws<NullReferenceException>(Problem);
        Assert.True(s_op1Evaluated);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte M43(Vector256<byte> arg1, ref byte[] arg2)
    {
        s_op1Evaluated = true;
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem()
    {
        Vector256<byte> vr18 = Vector256.Create<byte>(0);
        Vector256<byte> vr19 = Vector256.Create<byte>(0);
        byte vr20 = M43(vr19, ref s_27);
        Vector256<byte> vr21 = Vector256.CreateScalar(vr20);
        Vector256<byte> vr24 = s_53[0, 0];
        vr18 = Avx2.BlendVariable(vr21, vr24, vr18);
        M43(vr18, ref s_27);
    }
}
