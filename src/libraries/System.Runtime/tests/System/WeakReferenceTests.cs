// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public static unsafe class WeakReferenceTests
    {
        //
        // Helper method to create a weak reference that refers to a new object, without
        // accidentally keeping the object alive due to lifetime extension by the JIT.
        //
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MakeWeakReference(Func<object> valueFactory, bool trackResurrection = false)
        {
            return new WeakReference(valueFactory(), trackResurrection);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<object> MakeWeakReferenceOfObject(Func<object> valueFactory, bool trackResurrection = false)
        {
            return new WeakReference<object>(valueFactory(), trackResurrection);
        }

        [Fact]
        public static void NonGeneric()
        {
            object o1 = new char[10];
            WeakReference w = new WeakReference(o1);
            VerifyStillAlive(w);
            Assert.True(RuntimeHelpers.ReferenceEquals(o1, w.Target));
            Assert.False(w.TrackResurrection);
            GC.KeepAlive(o1);

            object o2 = new char[100];
            w.Target = o2;
            VerifyStillAlive(w);
            Assert.True(RuntimeHelpers.ReferenceEquals(o2, w.Target));
            GC.KeepAlive(o2);

            Latch l = new Latch();
            w = MakeWeakReference(() => new C(l));
            GC.Collect();
            VerifyIsDead(w);

            l = new Latch();
            w = MakeWeakReference(() => new ResurrectingC(l), true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (!l.FinalizerRan)
            {
                Console.WriteLine("Attempted GC but could not force test object to finalize. Test skipped.");
            }
            else
            {
                VerifyStillAlive(w);
            }

            l = new Latch();
            w = MakeWeakReference(() => new C(l), true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (!l.FinalizerRan)
            {
                Console.WriteLine("Attempted GC but could not force test object to finalize. Test skipped.");
            }
            else
            {
                VerifyIsDead(w);
            }
        }

        [Fact]
        public static void Generic()
        {
            object o1 = new char[10];
            WeakReference<object> w = new WeakReference<object>(o1);
            VerifyStillAlive(w);
            object v1;
            Assert.True(w.TryGetTarget(out v1));
            Assert.True(Object.ReferenceEquals(v1, o1));
            GC.KeepAlive(o1);

            object o2 = new char[100];
            w.SetTarget(o2);
            VerifyStillAlive(w);
            object v2;
            Assert.True(w.TryGetTarget(out v2));
            Assert.True(Object.ReferenceEquals(v2, o2));
            GC.KeepAlive(o2);

            Latch l = new Latch();
            w = MakeWeakReferenceOfObject(() => new C(l));
            GC.Collect();
            VerifyIsDead(w);

            l = new Latch();
            w = MakeWeakReferenceOfObject(() => new ResurrectingC(l), true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (!l.FinalizerRan)
            {
                Console.WriteLine("Attempted GC but could not force test object to finalize. Test skipped.");
            }
            else
            {
                VerifyStillAlive(w);
            }

            l = new Latch();
            w = MakeWeakReferenceOfObject(() => new C(l), true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (!l.FinalizerRan)
            {
                Console.WriteLine("Attempted GC but could not force test object to finalize. Test skipped.");
            }
            else
            {
                VerifyIsDead(w);
            }
        }

        private class Latch
        {
            public bool FinalizerRan;
        }

        private class C
        {
            public C(Latch latch)
            {
                _latch = latch;
            }

            ~C()
            {
                _latch.FinalizerRan = true;
            }

            private Latch _latch;
        }

        private static ResurrectingC s_resurrectedC;
        private class ResurrectingC
        {
            public ResurrectingC(Latch latch)
            {
                _latch = latch;
            }

            ~ResurrectingC()
            {
                _latch.FinalizerRan = true;
                s_resurrectedC = this;
            }

            private Latch _latch;
        }

        private static void VerifyStillAlive(WeakReference w)
        {
            Assert.True(w.IsAlive);
            Assert.True(w.Target != null);
        }

        private static void VerifyStillAlive<T>(WeakReference<T> w) where T : class
        {
            T value;
            bool isAlive = w.TryGetTarget(out value);
            Assert.True(isAlive);
            Assert.True(value != null);
        }

        private static void VerifyIsDead(WeakReference w)
        {
            Assert.False(w.IsAlive);
            Assert.Null(w.Target);
        }

        private static void VerifyIsDead<T>(WeakReference<T> w) where T : class
        {
            T value;
            bool isAlive = w.TryGetTarget(out value);
            Assert.False(isAlive);
            Assert.True(value == null);
        }
    }
}
