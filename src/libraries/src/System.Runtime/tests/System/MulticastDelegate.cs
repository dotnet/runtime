// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public static unsafe class MulticastDelegateTests
{
    [Fact]
    public static void TestGetInvocationList()
    {
        DFoo dfoo = new C().Foo;
        Delegate[] delegates = dfoo.GetInvocationList();
        Assert.NotNull(delegates);
        Assert.Equal(delegates.Length, 1);
        Assert.True(dfoo.Equals(delegates[0]));
    }

    [Fact]
    public static void TestEquals()
    {
        C c = new C();
        DFoo d1 = c.Foo;
        DFoo d2 = c.Foo;
        Assert.False(RuntimeHelpers.ReferenceEquals(d1, d2));
        bool b;
        b = d1.Equals(d2);
        Assert.True(b);
        Assert.True(d1 == d2);
        Assert.False(d1 != d2);

        d1 = c.Foo;
        d2 = c.Goo;
        b = d1.Equals(d2);
        Assert.False(b);
        Assert.False(d1 == d2);
        Assert.True(d1 != d2);

        d1 = new C().Foo;
        d2 = new C().Foo;
        b = d1.Equals(d2);
        Assert.False(b);
        Assert.False(d1 == d2);
        Assert.True(d1 != d2);

        DGoo dgoo = c.Foo;
        d1 = c.Foo;
        b = d1.Equals(dgoo);
        Assert.False(b);

        int h = d1.GetHashCode();
        int h2 = d1.GetHashCode();
        Assert.Equal(h, h2);
    }

    [Fact]
    public static void TestCombineReturn()
    {
        Tracker t = new Tracker();
        DRet dret1 = new DRet(t.ARet);
        DRet dret2 = new DRet(t.BRet);
        DRet dret12 = (DRet)Delegate.Combine(dret1, dret2);
        String s = dret12(4);
        Assert.Equal(s, "BRet4");
        Assert.Equal(t.S, "ARet4BRet4");
        return;
    }

    [Fact]
    public static void TestCombine()
    {
        Tracker t1 = new Tracker();
        D a = new D(t1.A);
        D b = new D(t1.B);
        D c = new D(t1.C);
        D d = new D(t1.D);

        D nothing = (D)(Delegate.Combine());
        Assert.Null(nothing);

        D one = (D)(Delegate.Combine(a));
        t1.Clear();
        one(5);
        Assert.Equal(t1.S, "A5");
        CheckInvokeList(new D[] { a }, one, t1);

        D ab = (D)(Delegate.Combine(a, b));
        t1.Clear();
        ab(5);
        Assert.Equal(t1.S, "A5B5");
        CheckInvokeList(new D[] { a, b }, ab, t1);

        D abc = (D)(Delegate.Combine(a, b, c));
        t1.Clear();
        abc(5);
        Assert.Equal(t1.S, "A5B5C5");
        CheckInvokeList(new D[] { a, b, c }, abc, t1);

        D abcdabc = (D)(Delegate.Combine(abc, d, abc));
        t1.Clear();
        abcdabc(9);
        Assert.Equal(t1.S, "A9B9C9D9A9B9C9");
        CheckInvokeList(new D[] { a, b, c, d, a, b, c }, abcdabc, t1);

        return;
    }

    [Fact]
    public static void TestRemove()
    {
        Tracker t1 = new Tracker();
        D a = new D(t1.A);
        D b = new D(t1.B);
        D c = new D(t1.C);
        D d = new D(t1.D);
        D e = new D(t1.E);

        D abc = (D)(Delegate.Combine(a, b, c));
        D abcdabc = (D)(Delegate.Combine(abc, d, abc));
        D ab = (D)(Delegate.Combine(a, b));
        D dab = (D)(Delegate.Combine(d, ab));
        D abcc = (D)(Delegate.Remove(abcdabc, dab));
        t1.Clear();
        abcc(9);
        String s = t1.S;
        Assert.Equal(s, "A9B9C9C9");
        CheckInvokeList(new D[] { a, b, c, c }, abcc, t1);

        // Pattern-match is based on structural equivalence, not reference equality.
        D acopy = new D(t1.A);
        D bbba = (D)(Delegate.Combine(b, b, b, acopy));
        D bbb = (D)(Delegate.Remove(bbba, a));
        t1.Clear();
        bbb(9);
        Assert.Equal(t1.S, "B9B9B9");
        CheckInvokeList(new D[] { b, b, b }, bbb, t1);

        // In the case of multiple occurrences, Remove() must remove the last one.
        D abcd = (D)(Delegate.Remove(abcdabc, abc));
        t1.Clear();
        abcd(9);
        Assert.Equal(t1.S, "A9B9C9D9");
        CheckInvokeList(new D[] { a, b, c, d }, abcd, t1);

        D d1 = (D)(Delegate.RemoveAll(abcdabc, abc));
        t1.Clear();
        d1(9);
        s = t1.S;
        Assert.Equal(t1.S, "D9");
        CheckInvokeList(new D[] { d }, d1, t1);

        D nothing = (D)(Delegate.Remove(d1, d1));
        Assert.Null(nothing);

        // The pattern-not-found case.
        D abcd1 = (D)(Delegate.Remove(abcd, null));
        t1.Clear();
        abcd1(9);
        Assert.Equal(t1.S, "A9B9C9D9");
        CheckInvokeList(new D[] { a, b, c, d }, abcd1, t1);

        // The pattern-not-found case.
        D abcd2 = (D)(Delegate.Remove(abcd, e));
        t1.Clear();
        abcd2(9);
        Assert.Equal(t1.S, "A9B9C9D9");
        CheckInvokeList(new D[] { a, b, c, d }, abcd2, t1);

        // The pattern-not-found case.
        D abcd3 = (D)(Delegate.RemoveAll(abcd, null));
        t1.Clear();
        abcd3(9);
        Assert.Equal(t1.S, "A9B9C9D9");
        CheckInvokeList(new D[] { a, b, c, d }, abcd3, t1);

        // The pattern-not-found case.
        D abcd4 = (D)(Delegate.RemoveAll(abcd, e));
        t1.Clear();
        abcd4(9);
        Assert.Equal(t1.S, "A9B9C9D9");
        CheckInvokeList(new D[] { a, b, c, d }, abcd4, t1);

        return;
    }

    private static void CheckInvokeList(D[] expected, D combo, Tracker target)
    {
        Delegate[] invokeList = combo.GetInvocationList();
        Assert.Equal(invokeList.Length, expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            CheckIsSingletonDelegate((D)(expected[i]), (D)(invokeList[i]), target);
        }
        Assert.True(Object.ReferenceEquals(combo.Target, expected[expected.Length - 1].Target));
        Assert.True(Object.ReferenceEquals(combo.Target, target));
    }

    private static void CheckIsSingletonDelegate(D expected, D actual, Tracker target)
    {
        Assert.True(expected.Equals(actual));
        Delegate[] invokeList = actual.GetInvocationList();
        Assert.Equal(invokeList.Length, 1);
        bool b = actual.Equals(invokeList[0]);
        Assert.True(b);

        target.Clear();
        expected(9);
        String sExpected = target.S;

        target.Clear();
        actual(9);
        String sActual = target.S;

        Assert.Equal(sExpected, sActual);

        Assert.True(Object.ReferenceEquals(target, actual.Target));
    }

    private class Tracker
    {
        public Tracker()
        {
            S = "";
        }

        public String S;

        public void Clear()
        {
            S = "";
        }

        public void A(int x)
        {
            S = S + "A" + x;
        }

        public String ARet(int x)
        {
            S = S + "ARet" + x;
            return "ARet" + x;
        }

        public void B(int x)
        {
            S = S + "B" + x;
        }

        public String BRet(int x)
        {
            S = S + "BRet" + x;
            return "BRet" + x;
        }

        public void C(int x)
        {
            S = S + "C" + x;
        }

        public void D(int x)
        {
            S = S + "D" + x;
        }

        public void E(int x)
        {
            S = S + "E" + x;
        }

        public void F(int x)
        {
            S = S + "F" + x;
        }
    }

    private delegate void D(int x);
    private delegate String DRet(int x);

    private delegate String DFoo(int x);
    private delegate String DGoo(int x);

    private class C
    {
        public String Foo(int x)
        {
            return new String('A', x);
        }

        public String Goo(int x)
        {
            return new String('A', x);
        }
    }
}

