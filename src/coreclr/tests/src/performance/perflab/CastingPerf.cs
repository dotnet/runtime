// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;

namespace PerfLabTests
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

    public interface IMyInterface1 { }
    public interface IMyInterface2 { }
    public class MyClass1 : IMyInterface1 { }
    public class MyClass2 : IMyInterface2 { }
    public class MyClass4<T> : IMyInterface1 { }

    public class CastingPerf
    {
        public const int NUM_ARRAY_ELEMENTS = 100;

        public static int[] j;
        public static int[] k;
        public static Foo[] foo;
        public static Foo2[] foo2;
        public static Foo[] n;
        public static Foo_5[] foo_5;
        public static FooSVT[] svt;
        public static FooORVT[] orvt;

        public static Object o;
        public static Object[] o_ar;
        public static Foo[] f;
        public static IFoo[] ifo;
        public static IFoo_5[] if_5;

        static CastingPerf()
        {
            j = new int[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < j.Length; i++)
            {
                j[i] = i;
            }
            foo = new Foo[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo.Length; i++)
            {
                foo[i] = new Foo();
            }
            foo2 = new Foo2[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo2.Length; i++)
            {
                foo2[i] = new Foo2();
            }
            n = new Foo[NUM_ARRAY_ELEMENTS];
            foo_5 = new Foo_5[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo_5.Length; i++)
            {
                foo_5[i] = new Foo_5();
            }
            svt = new FooSVT[NUM_ARRAY_ELEMENTS];
            orvt = new FooORVT[NUM_ARRAY_ELEMENTS];
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjFooIsObj()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o = foo;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjFooIsObj2()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o_ar = foo;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjObjIsFoo()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o_ar = (Object[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void FooObjIsFoo()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        f = (Foo[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void FooObjIsFoo2()
        {
            o_ar = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        f = (Foo[])o_ar;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void FooObjIsNull()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o = (Foo[])n;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void FooObjIsDescendant()
        {
            o = foo_5;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        f = (Foo[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void IFooFooIsIFoo()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        ifo = foo;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void IFooObjIsIFoo()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        ifo = (IFoo[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void IFooObjIsIFooInterAlia()
        {
            o = foo2;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        if_5 = (IFoo_5[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void IFooObjIsDescendantOfIFoo()
        {
            o = foo_5;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        ifo = (IFoo[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjInt()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o = (Object)j;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void IntObj()
        {
            o = (Object)j;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        k = (int[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjScalarValueType()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o = svt;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ScalarValueTypeObj()
        {
            o = svt;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        svt = (FooSVT[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjObjrefValueType()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        o = (Object)orvt;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void ObjrefValueTypeObj()
        {
            o = (Object)orvt;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        orvt = (FooORVT[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void FooObjCastIfIsa()
        {
            o = foo;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        if (o is Foo[])
                            f = (Foo[])o;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckObjIsInterfaceYes()
        {
            bool res = false;
            Object obj = new MyClass1();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = obj is IMyInterface1;
            return res;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckObjIsInterfaceNo()
        {
            bool res = false;
            Object obj = new MyClass2();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = obj is IMyInterface1;
            return res;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckIsInstAnyIsInterfaceYes()
        {
            bool res = false;
            Object obj = new MyClass4<List<string>>();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = obj is IMyInterface1;
            return res;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckIsInstAnyIsInterfaceNo()
        {
            bool res = false;
            Object obj = new MyClass4<List<string>>();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = obj is IMyInterface2;
            return res;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckArrayIsInterfaceYes()
        {
            bool res = false;
            Object[] arr = new MyClass1[5];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = arr is IMyInterface1[];
            return res;
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static bool CheckArrayIsInterfaceNo()
        {
            bool res = false;
            Object[] arr = new MyClass2[5];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        res = arr is IMyInterface1[];
            return res;
        }
    }
}