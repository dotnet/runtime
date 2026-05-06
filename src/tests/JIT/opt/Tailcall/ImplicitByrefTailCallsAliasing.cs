// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct S
{
    public long s_x;
    public long s_y;
}

public struct R
{
    public S r_s;
    public S r_t;
}

// Tail calls with implicit byref parameters as arguments.
//
// We need to ensure that we don't introduce aliased
// implicit byref parameters by optimizing away copies.

public class ImplicitByrefTailCalls
{
    // Helper method to make callees unattractive for inlining.
    internal static void Z() { }

    // Will return different answers if x and y refer to the same struct.
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias(S x, S y)
    {
        y.s_x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if y refers to some part of x.
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias2(S x, ref long y)
    {
        y++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if x and y refer to same struct
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias3(int a, int b, int c, int d, int e, int f, S x, S y)
    {
        y.s_x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if y refers to some part of x
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias4(ref long y, int b, int c, int d, int e, int f, S x)
    {
        y++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if x and r.s refer to the same struct.
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias5(S x, R r)
    {
        r.r_s.s_x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if ss and x refer to the same struct
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias6(Span<S> ss, S x)
    {
        ss[0].s_x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // Will return different answers if x and y refer to the same struct.
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static long Alias7(S x, ref S y)
    {
        y.s_x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.s_x++;
            result += x.s_x + x.s_y;
        }
        return result;
    }

    // A must copy params locally when calling Alias
    // and so can't tail call
    public static long A(S x)
    {
        Z(); Z(); Z(); Z();
        return Alias(x, x);
    }

    // B must copy params locally when calling Alias2
    // and so can't tail call
    public static long B(S x)
    {
        Z(); Z(); Z(); Z();
        return Alias2(x, ref x.s_y);
    }

    // C must copy params locally when calling Alias2
    // and so can't tail call. Here the problematic
    // tree is not part of the call.
    public static long C(S x)
    {
        ref long z = ref x.s_y;
        Z(); Z(); Z(); Z();
        return Alias2(x, ref z);
    }

    // D should not be able to tail call, as doing so
    // means we are not making local copies of x, and 
    // so introducing aliasing.
    public static long D(int a, int b, int c, int d, int e, int f, S x, S y)
    {
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        return Alias3(1, 2, 3, 4, 5, 6, x, x);
    }

    // E should be able to tail call
    public static long E(int a, int b, int c, int d, int e, int f, S x, S y)
    {
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        return Alias3(1, 2, 3, 4, 5, 6, x, y);
    }

    // F should be able to tail call
    public static long F(int a, int b, int c, int d, int e, int f, S x, S y)
    {
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        return Alias3(1, 2, 3, 4, 5, 6, y, x);
    }

    // G should be able to tail call, but we
    // might not want to pay the cost to prove it safe.
    public static long G(int a, int b, int c, int d, int e, int f, S x, S y)
    {
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();
        Z(); Z(); Z(); Z();

        if (a != 0)
        {
            return Alias4(ref x.s_x, 2, 3, 4, 5, 6, y);
        }
        else
        {
            return Alias4(ref y.s_x, 2, 3, 4, 5, 6, x);
        }
    }

    // H must copy params locally when calling Alias
    // and so can't tail call
    public static long H(R r)
    {
        Z(); Z(); Z(); Z();
        return Alias(r.r_s, r.r_s);
    }

    // I must copy params locally when calling Alias
    // and so can't tail call
    public static long I(R r)
    {
        Z(); Z(); Z(); Z();
        return Alias5(r.r_s, r);
    }

    // J can tail call, but we might not recognize this
    public static long J(R r)
    {
        Z(); Z(); Z(); Z();
        return Alias(r.r_s, r.r_t);
    }

    // K cannot tail call
    public static unsafe long K(S s)
    {
        Z(); Z(); Z(); Z();
        Span<S> ss = new Span<S>((void*)&s, 1);
        return Alias6(ss, s);
    }

    // L cannot tail call as the first arg to
    // Alias7 must be copied locally
    public static long L(S s)
    {
        Z(); Z(); Z(); Z();
        return Alias7(s, ref s);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S s = new S();
        s.s_x = 1;
        s.s_y = 2;
        R r = new R();
        r.r_s = s;
        r.r_t = s;
        long ra = A(s);
        long rb = B(s);
        long rc = C(s);
        long rd = D(0, 0, 0, 0, 0, 0, s, s);
        long re = E(0, 0, 0, 0, 0, 0, s, s);
        long rf = F(0, 0, 0, 0, 0, 0, s, s);
        long rg = G(0, 0, 0, 0, 0, 0, s, s);
        long rh = H(r);
        long ri = I(r);
        long rj = J(r);
        long rk = K(s);
        long rl = L(s);

        Console.WriteLine($"{ra},{rb},{rc},{rd},{re},{rf},{rg},{rh},{ri},{rj},{rk},{rl}");

        return
        (ra == 5350) && (rb == 5350) && (rc == 5350) && (rd == 5350) &&
        (re == 5350) && (rf == 5350) && (rg == 5350) && (rh == 5350) && 
        (ri == 5350) && (rj == 5350) && (rk == 5350) && (rl == 5350) ? 100 : -1;
    }
}
