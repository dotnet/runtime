// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoreFXTestLibrary;
using TypeOfRepo;


namespace ThreadLocalStatics
{
// disable "'field' is never used" warning (we have some static unused fields to test TS field offsets)
#pragma warning disable 0169

    public class MyType1<T>
    {
        static object _unused1;
        static double _unused2;
        [ThreadStatic]
        public static int _myField1;
        static float _unused3;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T1_SetField1(int s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int T1_GetField1() { return _myField1; }
    }
    public class MyDerived1<T> : MyType1<T>
    {
        static object _unused4;
        static float _unused5;
        [ThreadStatic]
        public static string _myField2;
        static float _unused6;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField1(int s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int T2_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField2(string s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string T2_GetField2() { return _myField2; }
    }
    public class MySuperDerived1_1<T> : MyDerived1<T>
    {
        static bool _unused7;
        [ThreadStatic]
        public static double _myField3;
        static char _unused8;
        static object _unused9;
        [ThreadStatic]
        public static string _myField4;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField1(int s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int T3_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField2(string s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string T3_GetField2() { return _myField2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField3(double s) { _myField3 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double T3_GetField3() { return _myField3; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField4(string s) { _myField4 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string T3_GetField4() { return _myField4; }
    }
    public class MySuperDerived1_2<T> : MyDerived1<T3>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T4_SetField1(int s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int T4_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T4_SetField2(string s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string T4_GetField2() { return _myField2; }
    }

    public class MyType2<T, U>
    {
        static object _unused1;
        static double _unused2;
        [ThreadStatic]
        public static T _myField1;
        [ThreadStatic]
        public static U _myField2;
        static bool _unused3;
        static char _unused4;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T1_SetField1(T s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T T1_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T1_SetField2(U s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static U T1_GetField2() { return _myField2; }
    }
    public class MyDerived2_1<T, U> : MyType2<T, U>
    {
        static string _unused5;
        static float _unused6;
        [ThreadStatic]
        public static T _myField3;
        [ThreadStatic]
        public static U _myField4;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField1(T s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T T2_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField2(U s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static U T2_GetField2() { return _myField2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField3(T s) { _myField3 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T T2_GetField3() { return _myField3; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T2_SetField4(U s) { _myField4 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static U T2_GetField4() { return _myField4; }
    }
    public class MyDerived2_2<T, U> : MyType2<string, double>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField1(string s) { _myField1 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string T3_GetField1() { return _myField1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void T3_SetField2(double s) { _myField2 = s; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double T3_GetField2() { return _myField2; }
    }

#if USC
    public struct T1
    {
        int _n;
        public T1(int n) { _n = n; }
        public override string ToString() { return "T1(" + _n + ")"; }
    }
    public struct T2
    {
        int _n;
        public T2(int n) { _n = n; }
        public override string ToString() { return "T2(" + _n + ")"; }
    }
    public struct T3 { }
    public struct T4
    {
        int _n;
        public T4(int n) { _n = n; }
        public override string ToString() { return "T4(" + _n + ")"; }
    }
    public struct T5
    {
        int _n;
        public T5(int n) { _n = n; }
        public override string ToString() { return "T5(" + _n + ")"; }
    }
#else
    public class T1
    {
        int _n;
        public T1(int n) { _n = n; }
        public override string ToString() { return "T1(" + _n + ")"; }
    }
    public class T2
    {
        int _n;
        public T2(int n) { _n = n; }
        public override string ToString() { return "T2(" + _n + ")"; }
    }
    public class T3 { }
    public class T4
    {
        int _n;
        public T4(int n) { _n = n; }
        public override string ToString() { return "T4(" + _n + ")"; }
    }
    public class T5
    {
        int _n;
        public T5(int n) { _n = n; }
        public override string ToString() { return "T5(" + _n + ")"; }
    }
#endif

#pragma warning restore 0169


    public class TLSTesting
    {
        static void InvokerHelper(MethodInfo[] setters, MethodInfo[] getters, object arg)
        {
            for (int i = 0; i < setters.Length; i++)
            {
                setters[i].Invoke(null, new object[] { arg });

                for (int j = 0; j < getters.Length; j++)
                {
                    var result = getters[j].Invoke(null, null);
                    Assert.AreEqual(arg.ToString(), result.ToString());
                }
            }
        }

        static void MakeType1(Type typeArg, bool checkInitialization = false)
        {
            var t1 = TypeOf.TLS_MyType1.MakeGenericType(typeArg);
            var t2 = TypeOf.TLS_MyDerived1.MakeGenericType(typeArg);
            var t3 = TypeOf.TLS_MySuperDerived1_1.MakeGenericType(typeArg);
            var t4 = TypeOf.TLS_MySuperDerived1_2.MakeGenericType(typeArg);

            var t1_set1 = t1.GetTypeInfo().GetDeclaredMethod("T1_SetField1");
            var t1_get1 = t1.GetTypeInfo().GetDeclaredMethod("T1_GetField1");

            var t2_set1 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField1");
            var t2_get1 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField1");
            var t2_set2 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField2");
            var t2_get2 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField2");

            var t3_set1 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField1");
            var t3_get1 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField1");
            var t3_set2 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField2");
            var t3_get2 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField2");
            var t3_set3 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField3");
            var t3_get3 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField3");
            var t3_set4 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField4");
            var t3_get4 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField4");

            var t4_set1 = t4.GetTypeInfo().GetDeclaredMethod("T4_SetField1");
            var t4_get1 = t4.GetTypeInfo().GetDeclaredMethod("T4_GetField1");
            var t4_set2 = t4.GetTypeInfo().GetDeclaredMethod("T4_SetField2");
            var t4_get2 = t4.GetTypeInfo().GetDeclaredMethod("T4_GetField2");

            var field1 = t1.GetTypeInfo().GetDeclaredField("_myField1");
            var field2 = t2.GetTypeInfo().GetDeclaredField("_myField2");
            var field3 = t3.GetTypeInfo().GetDeclaredField("_myField3");
            var field4 = t3.GetTypeInfo().GetDeclaredField("_myField4");

            if (checkInitialization)
            {
                Assert.AreEqual(0, (int)t1_get1.Invoke(null, null));
                Assert.AreEqual(0, (int)t2_get1.Invoke(null, null));
                Assert.AreEqual(0, (int)t3_get1.Invoke(null, null));

                Assert.IsNull(t2_get2.Invoke(null, null));
                Assert.IsNull(t3_get2.Invoke(null, null));

                Assert.AreEqual(0.0, (double)t3_get3.Invoke(null, null));
                Assert.IsNull(t3_get4.Invoke(null, null));
            }

            int ival = Environment.CurrentManagedThreadId | 0x123ABC00;
            string sval1 = "string1_thread#" + Environment.CurrentManagedThreadId;
            string sval2 = "string2_thread#" + Environment.CurrentManagedThreadId;
            double dval = (Environment.CurrentManagedThreadId | 1) * 15.123;

            // Test static function calls on the types. Static functions do some read/write operations on the TS fields
            {
                InvokerHelper(
                    new MethodInfo[] { t1_set1, t2_set1, t3_set1 },
                    new MethodInfo[] { t1_get1, t2_get1, t3_get1 },
                    ival);

                InvokerHelper(
                    new MethodInfo[] { t2_set2, t3_set2 },
                    new MethodInfo[] { t2_get2, t3_get2 },
                    sval1);

                InvokerHelper(
                    new MethodInfo[] { t3_set3 },
                    new MethodInfo[] { t3_get3 },
                    dval);

                InvokerHelper(
                    new MethodInfo[] { t3_set4 },
                    new MethodInfo[] { t3_get4 },
                    sval2);
            }

            // Test the FieldInfo.GetValue/SetValue APIs
            {
                Assert.AreEqual(ival, field1.GetValue(null));
                Assert.AreEqual(sval1, field2.GetValue(null));
                Assert.AreEqual(dval, field3.GetValue(null));
                Assert.AreEqual(sval2, field4.GetValue(null));

                ival = Environment.CurrentManagedThreadId + 13;
                sval1 = "f2_setvalue_thread#" + Environment.CurrentManagedThreadId;
                sval2 = "f4_setvalue_thread#" + Environment.CurrentManagedThreadId;
                dval = Environment.CurrentManagedThreadId * -15.3;
                field1.SetValue(null, ival);
                field2.SetValue(null, sval1);
                field3.SetValue(null, dval);
                field4.SetValue(null, sval2);
                Assert.AreEqual(ival, t3_get1.Invoke(null, null));
                Assert.AreEqual(sval1, t3_get2.Invoke(null, null));
                Assert.AreEqual(dval, t3_get3.Invoke(null, null));
                Assert.AreEqual(sval2, t3_get4.Invoke(null, null));
            }

            // Test dynamic type with statically known base type instantiations
            {
                MyType1<T3>._myField1 = Environment.CurrentManagedThreadId | 0xBADF00D;
                MyDerived1<T3>._myField2 = "string3_thread#" + Environment.CurrentManagedThreadId;
                Assert.AreEqual(MyType1<T3>._myField1, t4_get1.Invoke(null, null));
                Assert.AreEqual(MyDerived1<T3>._myField2, t4_get2.Invoke(null, null));

                t4_set1.Invoke(null, new object[] { Environment.CurrentManagedThreadId * 0x10101010 });
                t4_set2.Invoke(null, new object[] { "string4_thread#" + Environment.CurrentManagedThreadId });
                Assert.AreEqual(Environment.CurrentManagedThreadId * 0x10101010, MyType1<T3>._myField1);
                Assert.AreEqual("string4_thread#" + Environment.CurrentManagedThreadId, MyDerived1<T3>._myField2);
                Assert.AreEqual(MyType1<T3>._myField1, t4_get1.Invoke(null, null));
                Assert.AreEqual(MyDerived1<T3>._myField2, t4_get2.Invoke(null, null));
            }
        }

        static void MakeType2(Type typeArg1, Type typeArg2, bool checkInitialization = false)
        {
            var t1 = TypeOf.TLS_MyType2.MakeGenericType(typeArg1, typeArg2);
            var t2 = TypeOf.TLS_MyDerived2_1.MakeGenericType(typeArg1, typeArg2);
            var t3 = TypeOf.TLS_MyDerived2_2.MakeGenericType(typeArg1, typeArg2);

            var t1_set1 = t1.GetTypeInfo().GetDeclaredMethod("T1_SetField1");
            var t1_get1 = t1.GetTypeInfo().GetDeclaredMethod("T1_GetField1");
            var t1_set2 = t1.GetTypeInfo().GetDeclaredMethod("T1_SetField2");
            var t1_get2 = t1.GetTypeInfo().GetDeclaredMethod("T1_GetField2");

            var t2_set1 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField1");
            var t2_get1 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField1");
            var t2_set2 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField2");
            var t2_get2 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField2");
            var t2_set3 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField3");
            var t2_get3 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField3");
            var t2_set4 = t2.GetTypeInfo().GetDeclaredMethod("T2_SetField4");
            var t2_get4 = t2.GetTypeInfo().GetDeclaredMethod("T2_GetField4");

            var t3_set1 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField1");
            var t3_get1 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField1");
            var t3_set2 = t3.GetTypeInfo().GetDeclaredMethod("T3_SetField2");
            var t3_get2 = t3.GetTypeInfo().GetDeclaredMethod("T3_GetField2");

            var field1 = t1.GetTypeInfo().GetDeclaredField("_myField1");
            var field2 = t1.GetTypeInfo().GetDeclaredField("_myField2");
            var field3 = t2.GetTypeInfo().GetDeclaredField("_myField3");
            var field4 = t2.GetTypeInfo().GetDeclaredField("_myField4");

            if (checkInitialization)
            {
#if USC
                var default_typeArg1 = Activator.CreateInstance(typeArg1);
                var default_typeArg2 = Activator.CreateInstance(typeArg2);

                Assert.AreEqual(default_typeArg1, t1_get1.Invoke(null, null));
                Assert.AreEqual(default_typeArg1, t2_get1.Invoke(null, null));

                Assert.AreEqual(default_typeArg2, t1_get2.Invoke(null, null));
                Assert.AreEqual(default_typeArg2, t2_get2.Invoke(null, null));

                Assert.AreEqual(default_typeArg1, t2_get3.Invoke(null, null));
                Assert.AreEqual(default_typeArg2, t2_get4.Invoke(null, null));

#else
                Assert.IsNull(t1_get1.Invoke(null, null));
                Assert.IsNull(t2_get1.Invoke(null, null));

                Assert.IsNull(t1_get2.Invoke(null, null));
                Assert.IsNull(t2_get2.Invoke(null, null));

                Assert.IsNull(t2_get3.Invoke(null, null));
                Assert.IsNull(t2_get4.Invoke(null, null));
#endif
            }

            var t_obj = Activator.CreateInstance(typeArg1, new object[] { Environment.CurrentManagedThreadId });
            var u_obj = Activator.CreateInstance(typeArg2, new object[] { Environment.CurrentManagedThreadId });
            string sval = "teststring_thread#" + Environment.CurrentManagedThreadId;
            double dval = (Environment.CurrentManagedThreadId | 3) * 159.87;

            // Test static function calls on the types. Static functions do some read/write operations on the TS fields
            {
                InvokerHelper(
                    new MethodInfo[] { t1_set1, t2_set1 },
                    new MethodInfo[] { t1_get1, t2_get1 },
                    t_obj);

                InvokerHelper(
                    new MethodInfo[] { t1_set2, t2_set2 },
                    new MethodInfo[] { t1_get2, t2_get2 },
                    u_obj);

                InvokerHelper(
                    new MethodInfo[] { t2_set3 },
                    new MethodInfo[] { t2_get3 },
                    t_obj);

                InvokerHelper(
                    new MethodInfo[] { t2_set4 },
                    new MethodInfo[] { t2_get4 },
                    u_obj);
            }

            // Test the FieldInfo.GetValue/SetValue APIs
            {
                Assert.AreEqual(t_obj, field1.GetValue(null));
                Assert.AreEqual(u_obj, field2.GetValue(null));
                Assert.AreEqual(t_obj, field3.GetValue(null));
                Assert.AreEqual(u_obj, field4.GetValue(null));
            }

            // Test dynamic type with statically known base type instantiations
            {
                MyType2<string, double>._myField1 = "string3_thread#" + Environment.CurrentManagedThreadId;
                MyType2<string, double>._myField2 = 13.13 * 98.45 * (Environment.CurrentManagedThreadId | 5);
                Assert.AreEqual(MyType2<string, double>._myField1, t3_get1.Invoke(null, null));
                Assert.AreEqual(MyType2<string, double>._myField2, t3_get2.Invoke(null, null));

                t3_set1.Invoke(null, new object[] { "string4_thread#" + Environment.CurrentManagedThreadId });
                t3_set2.Invoke(null, new object[] { Environment.CurrentManagedThreadId * 0.189 + 1 });
                Assert.AreEqual("string4_thread#" + Environment.CurrentManagedThreadId, MyType2<string, double>._myField1);
                Assert.AreEqual(Environment.CurrentManagedThreadId * 0.189 + 1, MyType2<string, double>._myField2);
                Assert.AreEqual(MyType2<string, double>._myField1, t3_get1.Invoke(null, null));
                Assert.AreEqual(MyType2<string, double>._myField2, t3_get2.Invoke(null, null));
            }
        }

        public static void MultiThreaded_Test(Type typeArg1, Type typeArg2, int numTasks, int numIterations)
        {
            Task[] tasks = new Task[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < numIterations; j++)
                    {
                        MakeType1(typeArg1);
                        GC.Collect();
                        MakeType1(typeArg2);
                        GC.Collect();
                        MakeType2(typeArg1, typeArg2);
                        GC.Collect();
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        [TestMethod]
        public static void ThreadLocalStatics_Test()
        {
            for (int i = 0; i < 10; i++)
            {
                MakeType1(TypeOf.CommonType1, i == 0);
                MakeType1(TypeOf.CommonType2, i == 0);
                MakeType1(TypeOf.TLS_T1, i == 0);
                MakeType2(TypeOf.TLS_T1, TypeOf.TLS_T2, i == 0);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

#if DEBUG
            const int numTasks = 15;
#else
            const int numTasks = 20;
#endif
            MultiThreaded_Test(TypeOf.TLS_T4, TypeOf.TLS_T5, numTasks, 20);
        }
    }
}
