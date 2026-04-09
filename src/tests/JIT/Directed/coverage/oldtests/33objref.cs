// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//testing more than 32 (>33) objref's on the stack and as function arguments

using System;
using Xunit;

internal class ratnl
{
    private long _nmr,_dnm;
    public ratnl(long n, long d)
    {
        _nmr = n;
        _dnm = d;
    }
    public static ratnl operator +(ratnl a, ratnl b)
    {
        return new ratnl(a._nmr + b._nmr, a._dnm + b._nmr);
    }
    public static ratnl operator -(ratnl a, ratnl b)
    {
        return new ratnl(a._nmr - b._nmr, a._dnm - b._nmr);
    }
    public static ratnl operator *(ratnl a, ratnl b)
    {
        return new ratnl(a._nmr * b._nmr, a._dnm * b._nmr);
    }
    public long p_nmr
    {
        get { return _nmr; }
    }
    public long p_dnm
    {
        get { return _dnm; }
    }
}

public class Test33objref
{
    private static ratnl f1(ratnl a1, ratnl a2, ratnl a3, ratnl a4, ratnl a5, ratnl a6, ratnl a7, ratnl a8, ratnl a9, ratnl a10,
        ratnl a11, ratnl a12, ratnl a13, ratnl a14, ratnl a15, ratnl a16, ratnl a17, ratnl a18, ratnl a19, ratnl a20,
        ratnl a21, ratnl a22, ratnl a23, ratnl a24, ratnl a25, ratnl a26, ratnl a27, ratnl a28, ratnl a29, ratnl a30,
        ratnl a31, ratnl a32, ratnl a33)
    {
        ratnl result = ((a1 * a2 + a3 * a4 + a5 * a6 + a7 * a8 + a9 * a10) *
            (a11 - a12) * (a13 - a14) * (a15 - a16) * (a17 - a18) * (a19 - a20) -
            a30 - a29 + a28 - a27 + a26 - a25 + a24 - a23 + a22 - a21) *
            ((a33 - a30) * (a31 + a32));
        return result;
    }

    private static long f2(ratnl a1, ratnl a2, ratnl a3, ratnl a4, ratnl a5, ratnl a6, ratnl a7, ratnl a8, ratnl a9, ratnl a10,
        ratnl a11, ratnl a12, ratnl a13, ratnl a14, ratnl a15, ratnl a16, ratnl a17, ratnl a18, ratnl a19, ratnl a20,
        ratnl a21, ratnl a22, ratnl a23, ratnl a24, ratnl a25, ratnl a26, ratnl a27, ratnl a28, ratnl a29, ratnl a30,
        ratnl a31, ratnl a32, ratnl a33)
    {
        return ((a1.p_nmr * a2.p_nmr + a3.p_nmr * a4.p_nmr + a5.p_nmr * a6.p_nmr + a7.p_nmr * a8.p_nmr + a9.p_nmr * a10.p_nmr) *
            (a11.p_nmr - a12.p_nmr) * (a13.p_nmr - a14.p_nmr) * (a15.p_nmr - a16.p_nmr) * (a17.p_nmr - a18.p_nmr) * (a19.p_nmr - a20.p_nmr) -
            a30.p_nmr - a29.p_nmr + a28.p_nmr - a27.p_nmr + a26.p_nmr - a25.p_nmr + a24.p_nmr - a23.p_nmr + a22.p_nmr - a21.p_nmr) *
            (a33.p_nmr - a30.p_nmr) * (a31.p_nmr + a32.p_nmr);
    }

    private static long f3(ratnl a1, ratnl a2, ratnl a3, ratnl a4, ratnl a5, ratnl a6, ratnl a7, ratnl a8, ratnl a9, ratnl a10,
        ratnl a11, ratnl a12, ratnl a13, ratnl a14, ratnl a15, ratnl a16, ratnl a17, ratnl a18, ratnl a19, ratnl a20,
        ratnl a21, ratnl a22, ratnl a23, ratnl a24, ratnl a25, ratnl a26, ratnl a27, ratnl a28, ratnl a29, ratnl a30,
        ratnl a31, ratnl a32, ratnl a33)
    {
        return ((a1.p_dnm * a2.p_dnm + a3.p_dnm * a4.p_dnm + a5.p_dnm * a6.p_dnm + a7.p_dnm * a8.p_dnm + a9.p_dnm * a10.p_dnm) *
            (a11.p_dnm - a12.p_dnm) * (a13.p_dnm - a14.p_dnm) * (a15.p_dnm - a16.p_dnm) * (a17.p_dnm - a18.p_dnm) * (a19.p_dnm - a20.p_dnm) -
            a30.p_dnm - a29.p_dnm + a28.p_dnm - a27.p_dnm + a26.p_dnm - a25.p_dnm + a24.p_dnm - a23.p_dnm + a22.p_dnm - a21.p_dnm) *
            (a33.p_dnm - a30.p_dnm) * (a31.p_dnm + a32.p_dnm);
    }

    private static long f4(ref long a1, ref long a2, ref long a3, ref long a4, ref long a5, ref long a6, ref long a7, ref long a8, ref long a9, ref long a10,
        ref long a11, ref long a12, ref long a13, ref long a14, ref long a15, ref long a16, ref long a17, ref long a18, ref long a19, ref long a20,
        ref long a21, ref long a22, ref long a23, ref long a24, ref long a25, ref long a26, ref long a27, ref long a28, ref long a29, ref long a30,
        ref long a31, ref long a32, ref long a33)
    {
        return ((a1 * a2 + a3 * a4 + a5 * a6 + a7 * a8 + a9 * a10) *
            (a11 - a12) * (a13 - a14) * (a15 - a16) * (a17 - a18) * (a19 - a20) -
            a30 - a29 + a28 - a27 + a26 - a25 + a24 - a23 + a22 - a21) *
            (a33 - a30) * (a31 + a32);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;

        ratnl r1 = new ratnl(1, 1);
        ratnl r2 = new ratnl(2, 2);
        ratnl r3 = new ratnl(3, 3);
        ratnl r4 = new ratnl(4, 4);
        ratnl r5 = new ratnl(5, 5);
        ratnl r6 = new ratnl(6, 6);
        ratnl r7 = new ratnl(7, 7);
        ratnl r8 = new ratnl(8, 8);
        ratnl r9 = new ratnl(9, 9);
        ratnl r10 = new ratnl(10, 10);
        ratnl r11 = new ratnl(-10, 30);
        ratnl r12 = new ratnl(-9, 31);
        ratnl r13 = new ratnl(-8, 32);
        ratnl r14 = new ratnl(-7, 33);
        ratnl r15 = new ratnl(-6, 34);
        ratnl r16 = new ratnl(-5, 35);
        ratnl r17 = new ratnl(-4, 36);
        ratnl r18 = new ratnl(-3, 37);
        ratnl r19 = new ratnl(-2, 38);
        ratnl r20 = new ratnl(-1, 39);
        ratnl r21 = new ratnl(11, -1);
        ratnl r22 = new ratnl(22, -2);
        ratnl r23 = new ratnl(33, -3);
        ratnl r24 = new ratnl(44, -4);
        ratnl r25 = new ratnl(55, -5);
        ratnl r26 = new ratnl(66, -6);
        ratnl r27 = new ratnl(77, -7);
        ratnl r28 = new ratnl(88, -8);
        ratnl r29 = new ratnl(99, -9);
        ratnl r30 = new ratnl(30, -30);
        ratnl r31 = new ratnl(31, -31);
        ratnl r32 = new ratnl(32, -32);
        ratnl r33 = new ratnl(33, -33);

        if (f1(r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15, r16, r17, r18, r19, r20, r21, r22, r23, r24, r25, r26, r27, r28, r29, r30, r31, r32, r33).p_nmr
            != f2(r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15, r16, r17, r18, r19, r20, r21, r22, r23, r24, r25, r26, r27, r28, r29, r30, r31, r32, r33))
            passed = false;

        long dnm1 = r1.p_dnm;
        long dnm2 = r2.p_dnm;
        long dnm3 = r3.p_dnm;
        long dnm4 = r4.p_dnm;
        long dnm5 = r5.p_dnm;
        long dnm6 = r6.p_dnm;
        long dnm7 = r7.p_dnm;
        long dnm8 = r8.p_dnm;
        long dnm9 = r9.p_dnm;
        long dnm10 = r10.p_dnm;
        long dnm11 = r11.p_dnm;
        long dnm12 = r12.p_dnm;
        long dnm13 = r13.p_dnm;
        long dnm14 = r14.p_dnm;
        long dnm15 = r15.p_dnm;
        long dnm16 = r16.p_dnm;
        long dnm17 = r17.p_dnm;
        long dnm18 = r18.p_dnm;
        long dnm19 = r19.p_dnm;
        long dnm21 = r21.p_dnm;
        long dnm20 = r20.p_dnm;
        long dnm22 = r22.p_dnm;
        long dnm23 = r23.p_dnm;
        long dnm24 = r24.p_dnm;
        long dnm25 = r25.p_dnm;
        long dnm26 = r26.p_dnm;
        long dnm27 = r27.p_dnm;
        long dnm28 = r28.p_dnm;
        long dnm29 = r29.p_dnm;
        long dnm30 = r30.p_dnm;
        long dnm31 = r31.p_dnm;
        long dnm32 = r32.p_dnm;
        long dnm33 = r33.p_dnm;

        if (f3(r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15, r16, r17, r18, r19, r20, r21, r22, r23, r24, r25, r26, r27, r28, r29, r30, r31, r32, r33)
            != f4(ref dnm1, ref dnm2, ref dnm3, ref dnm4, ref dnm5, ref dnm6, ref dnm7, ref dnm8, ref dnm9, ref dnm10,
            ref dnm11, ref dnm12, ref dnm13, ref dnm14, ref dnm15, ref dnm16, ref dnm17, ref dnm18, ref dnm19, ref dnm20,
            ref dnm21, ref dnm22, ref dnm23, ref dnm24, ref dnm25, ref dnm26, ref dnm27, ref dnm28, ref dnm29, ref dnm30,
            ref dnm31, ref dnm32, ref dnm33))
            passed = false;

        if (!passed)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}




