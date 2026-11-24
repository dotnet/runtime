// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// SuperFileCheck derives JitDisasm method names from block-scoped namespaces only.
namespace Runtime_134474
{
    public struct Pair
    {
        public long A;
        public long B;
    }

    public class Inner
    {
        public Pair P;
    }

    public class Outer
    {
        public Inner I;
    }

    public class VolatileOuter
    {
        public volatile Inner I;
    }

    // A struct passed in multiple registers is loaded with one load per register.
    // The address of the struct (here o.I) must be computed once; re-reading it
    // for each register can observe different objects if o.I changes concurrently.
    public class Runtime_134474
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static long PassFieldOfField(Outer o)
        {
            // X64-LINUX:     gword ptr [{{[a-z0-9]+}}+0x08]
            // X64-LINUX-NOT: gword ptr [{{[a-z0-9]+}}+0x08]
            // X64-LINUX:     call
            return Use(o.I.P);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long PassFieldOfVolatileField(VolatileOuter o)
        {
            // X64-LINUX:     gword ptr [{{[a-z0-9]+}}+0x08]
            // X64-LINUX-NOT: gword ptr [{{[a-z0-9]+}}+0x08]
            // X64-LINUX:     call
            return Use(o.I.P);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Use(Pair p) => p.A * 10 + p.B;

        [Fact]
        public static void TestEntryPoint()
        {
            Inner inner = new Inner { P = new Pair { A = 1, B = 2 } };
            Assert.Equal(12, PassFieldOfField(new Outer { I = inner }));
            Assert.Equal(12, PassFieldOfVolatileField(new VolatileOuter { I = inner }));
        }
    }
}
