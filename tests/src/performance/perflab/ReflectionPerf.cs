// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xunit.Performance;
#pragma warning disable 67

namespace PerfLabTests
{
    public class GetMember
    {
        // all these fields will be initialized in init, so that they can be used directly in invocation
        private static readonly TypeInfo s_t1;
        private static readonly TypeInfo s_t2;
        private static readonly TypeInfo s_t3;
        private static readonly TypeInfo s_t4;
        private static readonly TypeInfo s_t5;
        private static readonly TypeInfo s_t6;
        private static readonly TypeInfo s_t7;
        private static readonly TypeInfo s_t8;
        private static readonly TypeInfo s_t9;
        private static readonly TypeInfo s_t10;
        private static readonly TypeInfo s_t11;
        private static readonly TypeInfo s_t12;
        private static readonly TypeInfo s_t13;
        private static readonly TypeInfo s_t14;
        private static readonly TypeInfo s_t15;
        private static readonly TypeInfo s_t16;
        private static readonly TypeInfo s_t17;
        private static readonly TypeInfo s_t18;
        private static readonly TypeInfo s_t19;
        private static readonly TypeInfo s_t20;

        static GetMember()
        {
            s_t1 = typeof(Class1).GetTypeInfo();
            s_t2 = typeof(Class2).GetTypeInfo();
            s_t3 = typeof(Class3).GetTypeInfo();
            s_t4 = typeof(Class4).GetTypeInfo();
            s_t5 = typeof(Class5).GetTypeInfo();
            s_t6 = typeof(Class6).GetTypeInfo();
            s_t7 = typeof(Class7).GetTypeInfo();
            s_t8 = typeof(Class8).GetTypeInfo();
            s_t9 = typeof(Class9).GetTypeInfo();
            s_t10 = typeof(Class10).GetTypeInfo();
            s_t11 = typeof(Class11).GetTypeInfo();
            s_t12 = typeof(Class12).GetTypeInfo();
            s_t13 = typeof(Class13).GetTypeInfo();
            s_t14 = typeof(Class14).GetTypeInfo();
            s_t15 = typeof(Class15).GetTypeInfo();
            s_t16 = typeof(Class16).GetTypeInfo();
            s_t17 = typeof(Class17).GetTypeInfo();
            s_t18 = typeof(Class18).GetTypeInfo();
            s_t19 = typeof(Class19).GetTypeInfo();
            s_t20 = typeof(Class20).GetTypeInfo();
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetField()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredField("f1");
                        s_t1.GetDeclaredField("f2");
                        s_t1.GetDeclaredField("f3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod1()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod2()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod3()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2"); //TODO: check if we can really get the method
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod4()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2"); //TODO: check if we can really get the method
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod5()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                        s_t5.GetDeclaredMethod("m1");
                        s_t5.GetDeclaredMethod("m2");
                        s_t5.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod10()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                        s_t5.GetDeclaredMethod("m1");
                        s_t5.GetDeclaredMethod("m2");
                        s_t5.GetDeclaredMethod("m3");

                        s_t6.GetDeclaredMethod("m1");
                        s_t6.GetDeclaredMethod("m2");
                        s_t6.GetDeclaredMethod("m3");
                        s_t7.GetDeclaredMethod("m1");
                        s_t7.GetDeclaredMethod("m2");
                        s_t7.GetDeclaredMethod("m3");
                        s_t8.GetDeclaredMethod("m1");
                        s_t8.GetDeclaredMethod("m2");
                        s_t8.GetDeclaredMethod("m3");
                        s_t9.GetDeclaredMethod("m1");
                        s_t9.GetDeclaredMethod("m2");
                        s_t9.GetDeclaredMethod("m3");
                        s_t10.GetDeclaredMethod("m1");
                        s_t10.GetDeclaredMethod("m2");
                        s_t10.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod12()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                        s_t5.GetDeclaredMethod("m1");
                        s_t5.GetDeclaredMethod("m2");
                        s_t5.GetDeclaredMethod("m3");

                        s_t6.GetDeclaredMethod("m1");
                        s_t6.GetDeclaredMethod("m2");
                        s_t6.GetDeclaredMethod("m3");
                        s_t7.GetDeclaredMethod("m1");
                        s_t7.GetDeclaredMethod("m2");
                        s_t7.GetDeclaredMethod("m3");
                        s_t8.GetDeclaredMethod("m1");
                        s_t8.GetDeclaredMethod("m2");
                        s_t8.GetDeclaredMethod("m3");
                        s_t9.GetDeclaredMethod("m1");
                        s_t9.GetDeclaredMethod("m2");
                        s_t9.GetDeclaredMethod("m3");
                        s_t10.GetDeclaredMethod("m1");
                        s_t10.GetDeclaredMethod("m2");
                        s_t10.GetDeclaredMethod("m3");

                        s_t11.GetDeclaredMethod("m1");
                        s_t11.GetDeclaredMethod("m2");
                        s_t11.GetDeclaredMethod("m3");
                        s_t12.GetDeclaredMethod("m1");
                        s_t12.GetDeclaredMethod("m2");
                        s_t12.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod15()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                        s_t5.GetDeclaredMethod("m1");
                        s_t5.GetDeclaredMethod("m2");
                        s_t5.GetDeclaredMethod("m3");

                        s_t6.GetDeclaredMethod("m1");
                        s_t6.GetDeclaredMethod("m2");
                        s_t6.GetDeclaredMethod("m3");
                        s_t7.GetDeclaredMethod("m1");
                        s_t7.GetDeclaredMethod("m2");
                        s_t7.GetDeclaredMethod("m3");
                        s_t8.GetDeclaredMethod("m1");
                        s_t8.GetDeclaredMethod("m2");
                        s_t8.GetDeclaredMethod("m3");
                        s_t9.GetDeclaredMethod("m1");
                        s_t9.GetDeclaredMethod("m2");
                        s_t9.GetDeclaredMethod("m3");
                        s_t10.GetDeclaredMethod("m1");
                        s_t10.GetDeclaredMethod("m2");
                        s_t10.GetDeclaredMethod("m3");

                        s_t11.GetDeclaredMethod("m1");
                        s_t11.GetDeclaredMethod("m2");
                        s_t11.GetDeclaredMethod("m3");
                        s_t12.GetDeclaredMethod("m1");
                        s_t12.GetDeclaredMethod("m2");
                        s_t12.GetDeclaredMethod("m3");
                        s_t13.GetDeclaredMethod("m1");
                        s_t13.GetDeclaredMethod("m2");
                        s_t13.GetDeclaredMethod("m3");
                        s_t14.GetDeclaredMethod("m1");
                        s_t14.GetDeclaredMethod("m2");
                        s_t14.GetDeclaredMethod("m3");
                        s_t15.GetDeclaredMethod("m1");
                        s_t15.GetDeclaredMethod("m2");
                        s_t15.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void GetMethod20()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        s_t1.GetDeclaredMethod("m1");
                        s_t1.GetDeclaredMethod("m2");
                        s_t1.GetDeclaredMethod("m3");
                        s_t2.GetDeclaredMethod("m1");
                        s_t2.GetDeclaredMethod("m2");
                        s_t2.GetDeclaredMethod("m3");
                        s_t3.GetDeclaredMethod("m1");
                        s_t3.GetDeclaredMethod("m2");
                        s_t3.GetDeclaredMethod("m3");
                        s_t4.GetDeclaredMethod("m1");
                        s_t4.GetDeclaredMethod("m2");
                        s_t4.GetDeclaredMethod("m3");
                        s_t5.GetDeclaredMethod("m1");
                        s_t5.GetDeclaredMethod("m2");
                        s_t5.GetDeclaredMethod("m3");

                        s_t6.GetDeclaredMethod("m1");
                        s_t6.GetDeclaredMethod("m2");
                        s_t6.GetDeclaredMethod("m3");
                        s_t7.GetDeclaredMethod("m1");
                        s_t7.GetDeclaredMethod("m2");
                        s_t7.GetDeclaredMethod("m3");
                        s_t8.GetDeclaredMethod("m1");
                        s_t8.GetDeclaredMethod("m2");
                        s_t8.GetDeclaredMethod("m3");
                        s_t9.GetDeclaredMethod("m1");
                        s_t9.GetDeclaredMethod("m2");
                        s_t9.GetDeclaredMethod("m3");
                        s_t10.GetDeclaredMethod("m1");
                        s_t10.GetDeclaredMethod("m2");
                        s_t10.GetDeclaredMethod("m3");

                        s_t11.GetDeclaredMethod("m1");
                        s_t11.GetDeclaredMethod("m2");
                        s_t11.GetDeclaredMethod("m3");
                        s_t12.GetDeclaredMethod("m1");
                        s_t12.GetDeclaredMethod("m2");
                        s_t12.GetDeclaredMethod("m3");
                        s_t13.GetDeclaredMethod("m1");
                        s_t13.GetDeclaredMethod("m2");
                        s_t13.GetDeclaredMethod("m3");
                        s_t14.GetDeclaredMethod("m1");
                        s_t14.GetDeclaredMethod("m2");
                        s_t14.GetDeclaredMethod("m3");
                        s_t15.GetDeclaredMethod("m1");
                        s_t15.GetDeclaredMethod("m2");
                        s_t15.GetDeclaredMethod("m3");

                        s_t16.GetDeclaredMethod("m1");
                        s_t16.GetDeclaredMethod("m2");
                        s_t16.GetDeclaredMethod("m3");
                        s_t17.GetDeclaredMethod("m1");
                        s_t17.GetDeclaredMethod("m2");
                        s_t17.GetDeclaredMethod("m3");
                        s_t18.GetDeclaredMethod("m1");
                        s_t18.GetDeclaredMethod("m2");
                        s_t18.GetDeclaredMethod("m3");
                        s_t19.GetDeclaredMethod("m1");
                        s_t19.GetDeclaredMethod("m2");
                        s_t19.GetDeclaredMethod("m3");
                        s_t20.GetDeclaredMethod("m1");
                        s_t20.GetDeclaredMethod("m2");
                        s_t20.GetDeclaredMethod("m3");
                    }
                }
            }
        }

        /*
        [Benchmark(InnerIterationCount=1000)]
        public static void GetConstructor()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    s_t1.GetConstructor(new Type[] { });
                    s_t1.GetConstructor(new Type[] { typeof(int) });
                    s_t1.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    s_t2.GetConstructor(new Type[] { });
                    s_t2.GetConstructor(new Type[] { typeof(int) });
                    s_t2.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    s_t3.GetConstructor(new Type[] { });
                    s_t3.GetConstructor(new Type[] { typeof(int) });
                    s_t3.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    s_t4.GetConstructor(new Type[] { });
                    s_t4.GetConstructor(new Type[] { typeof(int) });
                    s_t4.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    s_t5.GetConstructor(new Type[] { });
                    s_t5.GetConstructor(new Type[] { typeof(int) });
                    s_t5.GetConstructor(new Type[] { typeof(int), typeof(int) });
                }
            }
        }

        [Benchmark(InnerIterationCount=1000)]
        public static void GetProperty()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    PropertyInfo pi = s_t1.GetProperty("p1");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    pi = s_t1.GetProperty("p2");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    pi = s_t1.GetProperty("p3");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    pi = s_t2.GetProperty("p1");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    pi = s_t2.GetProperty("p2");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    pi = s_t2.GetProperty("p3");
                    pi.GetSetMethod();
                    pi.GetGetMethod();
                    s_t3.GetProperty("p1");
                    s_t3.GetProperty("p2");
                    s_t3.GetProperty("p3");
                    s_t4.GetProperty("p1");
                    s_t4.GetProperty("p2");
                    s_t4.GetProperty("p3");
                    s_t5.GetProperty("p1");
                    s_t5.GetProperty("p2");
                    s_t5.GetProperty("p3");
                }
            }
        }

        [Benchmark(InnerIterationCount=1000)]
        public static void GetEvent()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    EventInfo ei = s_t1.GetEvent("e1");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t1.GetEvent("e2");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t1.GetEvent("e3");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t2.GetEvent("e1");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t2.GetEvent("e2");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t2.GetEvent("e3");
                    ei.GetAddMethod();
                    ei.GetRaiseMethod();
                    ei.GetRemoveMethod();
                    s_t3.GetEvent("e1");
                    s_t3.GetEvent("e2");
                    s_t3.GetEvent("e3");
                    s_t4.GetEvent("e1");
                    s_t4.GetEvent("e2");
                    s_t4.GetEvent("e3");
                    s_t5.GetEvent("e1");
                    s_t5.GetEvent("e2");
                    s_t5.GetEvent("e3");
                }
            }
        }
        */
    }

    #region ClassDef
    public class Class1
    {
        public Class1() { }
        public Class1(int i) { }
        public Class1(int i, int ii) { }
        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }
    public class Class2
    {
        public Class2() { }
        public Class2(int i) { }
        public Class2(int i, int ii) { }
        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class3
    {
        public Class3() { }
        public Class3(int i) { }
        public Class3(int i, int ii) { }
        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class4
    {
        public Class4() { }
        public Class4(int i) { }
        public Class4(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class5
    {
        public Class5() { }
        public Class5(int i) { }
        public Class5(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class6
    {
        public Class6() { }
        public Class6(int i) { }
        public Class6(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class7
    {
        public Class7() { }
        public Class7(int i) { }
        public Class7(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class8
    {
        public Class8() { }
        public Class8(int i) { }
        public Class8(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class9
    {
        public Class9() { }
        public Class9(int i) { }
        public Class9(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class10
    {
        public Class10() { }
        public Class10(int i) { }
        public Class10(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class11
    {
        public Class11() { }
        public Class11(int i) { }
        public Class11(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class12
    {
        public Class12() { }
        public Class12(int i) { }
        public Class12(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class13
    {
        public Class13() { }
        public Class13(int i) { }
        public Class13(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class14
    {
        public Class14() { }
        public Class14(int i) { }
        public Class14(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class15
    {
        public Class15() { }
        public Class15(int i) { }
        public Class15(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class16
    {
        public Class16() { }
        public Class16(int i) { }
        public Class16(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class17
    {
        public Class17() { }
        public Class17(int i) { }
        public Class17(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class18
    {
        public Class18() { }
        public Class18(int i) { }
        public Class18(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class19
    {
        public Class19() { }
        public Class19(int i) { }
        public Class19(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }

    public class Class20
    {
        public Class20() { }
        public Class20(int i) { }
        public Class20(int i, int ii) { }

        public int f0 = 0;
        static public int f1 = 0;
        public int f2 = 0;
        public int f3 = 0;
        public int f4 = 0;
        public int f5 = 0;
        public int f6 = 0;
        public int f7 = 0;
        public int f8 = 0;
        public int f9 = 0;
        public int f10 = 0;
        public int f11 = 0;
        public int f12 = 0;
        public int f13 = 0;
        public int f14 = 0;
        public int f15 = 0;
        public int f16 = 0;
        public int f17 = 0;
        public int f18 = 0;
        public int f19 = 0;
        public int f20 = 0;
        public int f21 = 0;
        public int f22 = 0;
        public int f23 = 0;
        public int f24 = 0;

        public void m1() { }
        static public void m2() { }
        public void m3() { }
        public void m4() { }
        public void m5() { }
        public void m6() { }
        public void m7() { }
        public void m8() { }
        public void m9() { }
        public void m10() { }
        public void m11() { }
        public void m12() { }
        public void m13() { }
        public void m14() { }
        public void m15() { }
        public void m16() { }
        public void m17() { }
        public void m18() { }
        public void m19() { }
        public void m20() { }
        public void m21() { }
        public void m22() { }
        public void m23() { }
        public void m24() { }

        public int p0 { get { return 1; } set { } }
        static public int p1 { get { return 1; } set { } }
        public int p2 { get { return 1; } set { } }
        public int p3 { get { return 1; } set { } }
        public int p4 { get { return 1; } set { } }
        public int p5 { get { return 1; } set { } }
        public int p6 { get { return 1; } set { } }
        public int p7 { get { return 1; } set { } }
        public int p8 { get { return 1; } set { } }
        public int p9 { get { return 1; } set { } }
        public int p10 { get { return 1; } set { } }
        public int p11 { get { return 1; } set { } }
        public int p12 { get { return 1; } set { } }
        public int p13 { get { return 1; } set { } }
        public int p14 { get { return 1; } set { } }
        public int p15 { get { return 1; } set { } }
        public int p16 { get { return 1; } set { } }
        public int p17 { get { return 1; } set { } }
        public int p18 { get { return 1; } set { } }
        public int p19 { get { return 1; } set { } }
        public int p20 { get { return 1; } set { } }
        public int p21 { get { return 1; } set { } }
        public int p22 { get { return 1; } set { } }
        public int p23 { get { return 1; } set { } }
        public int p24 { get { return 1; } set { } }

        public event d e0;
        static public event d e1;
        public event d e2;
        public event d e3;
        public event d e4;
        public event d e5;
        public event d e6;
        public event d e7;
        public event d e8;
        public event d e9;
        public event d e10;
        public event d e11;
        public event d e12;
        public event d e13;
        public event d e14;
        public event d e15;
        public event d e16;
        public event d e17;
        public event d e18;
        public event d e19;
        public event d e20;
        public event d e21;
        public event d e22;
        public event d e23;
        public event d e24;

        public void NoWarning()
        {
            e0 += new d(m1);
            e1 += new d(m1);
            e2 += new d(m1);
            e3 += new d(m1);
            e4 += new d(m1);
            e5 += new d(m1);
            e6 += new d(m1);
            e7 += new d(m1);
            e8 += new d(m1);
            e9 += new d(m1);
            e10 += new d(m1);
            e11 += new d(m1);
            e12 += new d(m1);
            e13 += new d(m1);
            e14 += new d(m1);
            e15 += new d(m1);
            e16 += new d(m1);
            e17 += new d(m1);
            e18 += new d(m1);
            e19 += new d(m1);
            e20 += new d(m1);
            e21 += new d(m1);
            e22 += new d(m1);
            e23 += new d(m1);
            e24 += new d(m1);
        }
    }
    #endregion


    public delegate void d();
}