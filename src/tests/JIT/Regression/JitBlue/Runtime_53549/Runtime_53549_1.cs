// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

class A
{
    public static string I(FormattableString formattable)
    {
        return FormattableString.Invariant(formattable);
    }

    public virtual decimal X => d;
    public decimal D => X;
    public decimal P => p;
    public string S => "S";
    public decimal d;
    public decimal p;

    // This method would produce improper code with GDV
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string ToString() => I($"{S} {D} {P}");
}

class B : A
{
    public override decimal X => d + 1;
}

public class Repro
{
    static string s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int F(A[] a)
    {
        int i = 0;
        for (; i < a.Length; i++)
        {
            s = a[i].ToString();
        }

        return i;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        A[] a = new A[1000];

        for (int i = 0; i < a.Length; i++)
        {
            a[i] = new B();
        }

        for (int j = 0; j < 50; j++)
        {
            F(a);
            Thread.Sleep(15);
        }

        Thread.Sleep(100);

        int r = F(a);

        Console.WriteLine($"Result is {s} after {r} iterations\n");

        return r / 10;
    }
}
