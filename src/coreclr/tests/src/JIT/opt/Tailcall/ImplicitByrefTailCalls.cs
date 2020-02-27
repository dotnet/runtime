// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Tail calls with implicit byref parameters as arguments.
//
// Generally, we can tail call provided we don't copy the 
// argument to the local frame.

public class ImplicitByrefTailCalls
{
    public static void Z() {}
    public static bool Z(bool b) => b;

    public static int ZZ(Span<int> x)
    {
        int result = 0;
        for (int i = 0; i < x.Length; i++)
        {
            result += x[i] * x[i] + i;
        }
        return result;
    }

    public static int ZZZ(Span<int> x, int a, int b, int c, int d, int e, int f)
    {
        Z(); Z();
        Z(); Z();
        int result = 0;
        for (int i = 0; i < x.Length; i++)
        {
            result += x[i] * x[i] + i;
        }
        return result;
    }

    // Various methods that should fast tail call

    public static int A(Span<int> x)
    {
        Z(); Z(); return ZZ(x);
    } 

    public static int B(Span<int> x)
    {
        Z(); Z(); return A(x);
    }

    public static int C(Span<int> x, bool p)
    {
        Z(); Z();
        if (Z(p))
        {
            return A(x);
        }
        else
        {
            return B(x);
        }
    }

    public static int D(Span<int> x, bool p)
    {
        Z(); Z();
        return Z(p) ? A(x) : B(x);
    }

    public static int E(Span<int> x, Span<int> y, bool p)
    {
        Z(); Z(); Z(); Z();
        if (Z(p))
        {
            return A(x);
        }
        else
        {
            return B(y);
        }
    }

    public static int F(Span<int> x, Span<int> y, bool p)
    {
        Z(); Z(); Z(); Z();
        return Z(p) ? ZZ(x) : ZZ(y);
    }

    // Methods with tail call sites and other uses

    public static int G(Span<int> x, bool p)
    {
        Z(); Z();
        if (Z(p))
        {
            return ZZ(x);
        }
        else
        {
            return x.Length;
        }
    }

    // Here there are enough actual uses to
    // justify promotion, hence we can't tail
    // call, as "undoing" promotion at the end
    // entails making a local copy.
    //
    // We could handle this by writing the updated 
    // struct values back to the original byref
    // before tail calling....
    public static int H(Span<int> x, int y)
    {
        Z(); Z();
        if (y < x.Length)
        {
            int result = 0;
            for (int i = 0; i < y; i++)
            {
                result += x[i];
            }
            return result;
        }

        return ZZ(x);
    }

    // Here the call to ZZZ would need to be a slow tail call,
    // so we won't tail call at all. But the only
    // reference to x is at the call, and it's not in 
    // a loop, so we can still can avoid making a copy.
    public static int S(Span<int> x)
    {
        Z(); Z();
        return ZZZ(x, 1, 2, 3, 4, 5, 6);
    } 

    // Must copy at the normal call site
    // but can avoid at tail call site
    public static int T(Span<int> x, ref int q)
    {
        Z(); Z();
        q = ZZ(x);
        return ZZ(x);
    }

    static bool p;

    public static int Main()
    {
        int[] a = new int[100];
        a[45] = 55;
        a[55] = 45;
        p = false;

        Span<int> s = new Span<int>(a);
        int q = 0;

        int ra = A(s);
        int rb = B(s);
        int rc = C(s, p);
        int rd = D(s, p);
        int re = E(s, s, p);
        int rf = F(s, s, p);
        int rg = G(s, p);
        int rh = H(s, 46);
        int rs = S(s);
        int rt = T(s, ref q);

        Console.WriteLine($"{ra},{rb},{rc},{rd},{re},{rf},{rg},{rh},{rs},{rt}");
        
        return ra + rb + rc + rd + re + rf + rg + rh + rs + rt - 80055;
    }
}
