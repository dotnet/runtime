// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static unsafe class MulticastDelegateTests
    {
        [Fact]
        public static void GetInvocationList()
        {
            DFoo dfoo = new C().Foo;
            Delegate[] delegates = dfoo.GetInvocationList();
            Assert.NotNull(delegates);
            Assert.Equal(delegates.Length, 1);
            Assert.True(dfoo.Equals(delegates[0]));
        }

        [Fact]
        public static void Equals()
        {
            C c = new C();
            DFoo d1 = c.Foo;
            DFoo d2 = c.Foo;
            Assert.NotSame(d1, d2);
            Assert.Equal(d1, d2);
            Assert.True(d1 == d2);
            Assert.False(d1 != d2);

            d1 = c.Foo;
            d2 = c.Goo;
            Assert.NotEqual(d1, d2);
            Assert.False(d1 == d2);
            Assert.True(d1 != d2);

            d1 = new C().Foo;
            d2 = new C().Foo;
            Assert.NotEqual(d1, d2);
            Assert.False(d1 == d2);
            Assert.True(d1 != d2);

            DGoo dgoo = c.Foo;
            d1 = c.Foo;
            Assert.NotEqual(d1, (object)dgoo);
            
            Assert.Equal(d1.GetHashCode(), d1.GetHashCode());
        }

        [Fact]
        public static void CombineReturn()
        {
            Tracker t = new Tracker();
            DRet dret1 = new DRet(t.ARet);
            DRet dret2 = new DRet(t.BRet);
            DRet dret12 = (DRet)Delegate.Combine(dret1, dret2);
            string s = dret12(4);
            Assert.Equal(s, "BRet4");
            Assert.Equal(t.S, "ARet4BRet4");
        }

        [Fact]
        public static void Combine()
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
        }

        [Fact]
        public static void Remove()
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
            string s = t1.S;
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
        }

        private static void CheckInvokeList(D[] expected, D combo, Tracker target)
        {
            Delegate[] invokeList = combo.GetInvocationList();
            Assert.Equal(invokeList.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                CheckIsSingletonDelegate((D)(expected[i]), (D)(invokeList[i]), target);
            }
            Assert.Same(combo.Target, expected[expected.Length - 1].Target);
            Assert.Same(combo.Target, target);
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
            string sExpected = target.S;

            target.Clear();
            actual(9);
            string sActual = target.S;

            Assert.Equal(sExpected, sActual);

            Assert.Same(target, actual.Target);
        }

        private class Tracker
        {
            public Tracker()
            {
                S = "";
            }

            public string S;

            public void Clear() => S = "";

            public void A(int x) => S = S + "A" + x;

            public string ARet(int x)
            {
                S = S + "ARet" + x;
                return "ARet" + x;
            }

            public void B(int x) => S = S + "B" + x;

            public string BRet(int x)
            {
                S = S + "BRet" + x;
                return "BRet" + x;
            }

            public void C(int x) => S = S + "C" + x;

            public void D(int x) => S = S + "D" + x;

            public void E(int x) => S = S + "E" + x;

            public void F(int x) => S = S + "F" + x;
        }

        private delegate void D(int x);
        private delegate string DRet(int x);

        private delegate string DFoo(int x);
        private delegate string DGoo(int x);

        private class C
        {
            public string Foo(int x) => new string('A', x);

            public string Goo(int x) => new string('A', x);
        }
    }
}
