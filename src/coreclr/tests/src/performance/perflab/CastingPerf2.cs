// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CastingPerf2;
using Microsoft.Xunit.Performance;

namespace CastingPerf2
{
    public interface IFoo
    {
    }

    public interface IFoo_1
    {
    }

    public interface IFoo_2
    {
    }

    public interface IFoo_3
    {
    }

    public interface IFoo_4
    {
    }

    public interface IFoo_5
    {
    }

    // C# lays the interfaces in reverse order in metadata. So IFoo is the first and IFoo_5 is last
    public class Foo : IFoo_5, IFoo_4, IFoo_3, IFoo_2, IFoo_1, IFoo
    {
        public int m_i;
    }

    public class Foo_1 : Foo
    {
        public int m_j;
    }

    public class Foo_2 : Foo_1
    {
        public int m_k;
    }

    public class Foo_3 : Foo_2
    {
        public int m_l;
    }

    public class Foo_4 : Foo_3
    {
        public int m_m;
    }

    public class Foo_5 : Foo_4
    {
        public int m_n;
    }

    // C# lays the interfaces in reverse order in metadata. So IFoo_1 is the first and IFoo is last
    public class Foo2 : IFoo, IFoo_5, IFoo_4, IFoo_3, IFoo_2, IFoo_1
    {
        public int m_i;
    }

    public struct FooSVT
    {
        public int m_i;
        public int m_j;
    }

    public struct FooORVT
    {
        public Object m_o;
        public Foo m_f;
    }

    public class CastingPerf
    {
        public static int j, j1, j2, j3, j4, j5, j6, j7, j8, j9;
        public static Foo foo = new Foo();
        public static Foo2 foo2 = new Foo2();
        public static Foo n = null;
        public static Foo_5 foo_5 = new Foo_5();
        public static FooSVT svt = new FooSVT();
        public static FooORVT orvt = new FooORVT();

        public static Object o, o1, o2, o3, o4, o5, o6, o7, o8, o9;
        public static Foo f, f1, f2, f3, f4, f5, f6, f7, f8, f9;
        public static IFoo ifo, ifo1, ifo2, ifo3, ifo4, ifo5, ifo6, ifo7, ifo8, ifo9;
        public static IFoo_5 if_0, if_1, if_2, if_3, if_4, if_5, if_6, if_7, if_8, if_9;

        [Benchmark]
        public static void ObjFooIsObj()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    o = foo;
        }

        [Benchmark]
        public static void FooObjIsFoo()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    f = (Foo)o;
        }

        [Benchmark]
        public static void FooObjIsNull()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    o = (Foo)n;
        }

        [Benchmark]
        public static void FooObjIsDescendant()
        {
            o = foo_5;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    f = (Foo)o;
        }

        [Benchmark]
        public static void IFooFooIsIFoo()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ifo = foo;
        }

        [Benchmark]
        public static void IFooObjIsIFoo()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ifo = (IFoo)o;
        }

        [Benchmark]
        public static void IFooObjIsIFooInterAlia()
        {
            o = foo2;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    if_0 = (IFoo_5)o;
        }

        [Benchmark]
        public static void IFooObjIsDescendantOfIFoo()
        {
            o = foo_5;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ifo = (IFoo)o;
        }

        [Benchmark]
        public static void ObjInt()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    o = (Object)j;
        }

        [Benchmark]
        public static void IntObj()
        {
            o = (Object)1;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    j = (int)o;
        }

        [Benchmark]
        public static void ObjScalarValueType()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    o = svt;
        }

        [Benchmark]
        public static void ScalarValueTypeObj()
        {
            o = svt;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    svt = (FooSVT)o;
        }

        [Benchmark]
        public static void ObjObjrefValueType()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    o = (Object)orvt;
        }

        [Benchmark]
        public static void ObjrefValueTypeObj()
        {
            o = (Object)orvt;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    orvt = (FooORVT)o;
        }

        [Benchmark]
        public static void FooObjCastIfIsa()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    if (o is Foo)
                        f = (Foo)o;
        }
    }
}