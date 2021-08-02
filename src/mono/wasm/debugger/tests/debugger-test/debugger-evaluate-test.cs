// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class EvaluateTestsClass
    {
        public class TestEvaluate
        {
            public int a;
            public int b;
            public int c;
            public DateTime dt = new DateTime(2000, 5, 4, 3, 2, 1);
            public TestEvaluate NullIfAIsNotZero => a != 0 ? null : this;
            public void run(int g, int h, string a, string valString, int this_a)
            {
                int d = g + 1;
                int e = g + 2;
                int f = g + 3;
                int i = d + e + f;
                var local_dt = new DateTime(2010, 9, 8, 7, 6, 5);
                this.a = 1;
                b = 2;
                c = 3;
                this.a = this.a + 1;
                b = b + 1;
                c = c + 1;
            }
        }

        public static void EvaluateLocals()
        {
            TestEvaluate f = new TestEvaluate();
            f.run(100, 200, "9000", "test", 45);
            var f_s = new EvaluateTestsStructWithProperties();
            f_s.InstanceMethod(100, 200, "test", f_s);
            f_s.GenericInstanceMethod<int>(100, 200, "test", f_s);

            var f_g_s = new EvaluateTestsGenericStruct<int>();
            f_g_s.EvaluateTestsGenericStructInstanceMethod(100, 200, "test");
        }

    }

    public struct EvaluateTestsGenericStruct<T>
    {
        public int a;
        public int b;
        public int c;
        DateTime dateTime;
        public void EvaluateTestsGenericStructInstanceMethod(int g, int h, string valString)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            T t = default(T);
            a = a + 1;
            b = b + 2;
            c = c + 3;
        }
    }

    public class EvaluateTestsClassWithProperties
    {
        public int a;
        public int b;
        public int c { get; set; }

        public DateTime dateTime;
        public DateTime DTProp => dateTime.AddMinutes(10);
        public int IntProp => a + 5;
        public string PropertyThrowException => throw new Exception("error");
        public string SetOnlyProp { set { a = value.Length; } }
        public EvaluateTestsClassWithProperties NullIfAIsNotZero => a != 1908712 ? null : new EvaluateTestsClassWithProperties(0);
        public EvaluateTestsClassWithProperties NewInstance => new EvaluateTestsClassWithProperties(3);

        public EvaluateTestsClassWithProperties(int bias)
        {
            a = 4;
            b = 0;
            c = 0;
            dateTime = new DateTime(2010, 9, 8, 7, 6, 5 + bias);
        }

        public static async Task run()
        {
            var obj = new EvaluateTestsClassWithProperties(0);
            var obj2 = new EvaluateTestsClassWithProperties(0);
            obj.InstanceMethod(400, 123, "just a test", obj2);
            new EvaluateTestsClassWithProperties(0).GenericInstanceMethod<int>(400, 123, "just a test", obj2);
            new EvaluateTestsClassWithProperties(0).EvaluateShadow(new DateTime(2020, 3, 4, 5, 6, 7), obj.NewInstance);

            await new EvaluateTestsClassWithProperties(0).InstanceMethodAsync(400, 123, "just a test", obj2);
            await new EvaluateTestsClassWithProperties(0).GenericInstanceMethodAsync<int>(400, 123, "just a test", obj2);
            await new EvaluateTestsClassWithProperties(0).EvaluateShadowAsync(new DateTime(2020, 3, 4, 5, 6, 7), obj.NewInstance);
        }

        public void EvaluateShadow(DateTime dateTime, EvaluateTestsClassWithProperties me)
        {
            string a = "hello";
            Console.WriteLine($"Evaluate - break here");
            SomeMethod(dateTime, me);
        }

        public async Task EvaluateShadowAsync(DateTime dateTime, EvaluateTestsClassWithProperties me)
        {
            string a = "hello";
            Console.WriteLine($"EvaluateShadowAsync - break here");
            await Task.CompletedTask;
        }

        public void SomeMethod(DateTime me, EvaluateTestsClassWithProperties dateTime)
        {
            Console.WriteLine($"break here");

            var DTProp = "hello";
            Console.WriteLine($"dtProp: {DTProp}");
        }

        public async Task InstanceMethodAsync(int g, int h, string valString, EvaluateTestsClassWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            await Task.CompletedTask;
        }

        public void InstanceMethod(int g, int h, string valString, EvaluateTestsClassWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
        }

        public void GenericInstanceMethod<T>(int g, int h, string valString, EvaluateTestsClassWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            T t = default(T);
        }

        public async Task<T> GenericInstanceMethodAsync<T>(int g, int h, string valString, EvaluateTestsClassWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            T t = default(T);
            return await Task.FromResult(default(T));
        }
    }

    public struct EvaluateTestsStructWithProperties
    {
        public int a;
        public int b;
        public int c { get; set; }

        public DateTime dateTime;
        public DateTime DTProp => dateTime.AddMinutes(10);
        public int IntProp => a + 5;
        public string SetOnlyProp { set { a = value.Length; } }
        public EvaluateTestsClassWithProperties NullIfAIsNotZero => a != 1908712 ? null : new EvaluateTestsClassWithProperties(0);
        public EvaluateTestsStructWithProperties NewInstance => new EvaluateTestsStructWithProperties(3);

        public EvaluateTestsStructWithProperties(int bias)
        {
            a = 4;
            b = 0;
            c = 0;
            dateTime = new DateTime(2010, 9, 8, 7, 6, 5 + bias);
        }

        public static async Task run()
        {
            var obj = new EvaluateTestsStructWithProperties(0);
            var obj2 = new EvaluateTestsStructWithProperties(0);
            obj.InstanceMethod(400, 123, "just a test", obj2);
            new EvaluateTestsStructWithProperties(0).GenericInstanceMethod<int>(400, 123, "just a test", obj2);
            new EvaluateTestsStructWithProperties(0).EvaluateShadow(new DateTime(2020, 3, 4, 5, 6, 7), obj.NewInstance);

            await new EvaluateTestsStructWithProperties(0).InstanceMethodAsync(400, 123, "just a test", obj2);
            await new EvaluateTestsStructWithProperties(0).GenericInstanceMethodAsync<int>(400, 123, "just a test", obj2);
            await new EvaluateTestsStructWithProperties(0).EvaluateShadowAsync(new DateTime(2020, 3, 4, 5, 6, 7), obj.NewInstance);
        }

        public void EvaluateShadow(DateTime dateTime, EvaluateTestsStructWithProperties me)
        {
            string a = "hello";
            Console.WriteLine($"Evaluate - break here");
            SomeMethod(dateTime, me);
        }

        public async Task EvaluateShadowAsync(DateTime dateTime, EvaluateTestsStructWithProperties me)
        {
            string a = "hello";
            Console.WriteLine($"EvaluateShadowAsync - break here");
            await Task.CompletedTask;
        }

        public void SomeMethod(DateTime me, EvaluateTestsStructWithProperties dateTime)
        {
            Console.WriteLine($"break here");

            var DTProp = "hello";
            Console.WriteLine($"dtProp: {DTProp}");
        }

        public async Task InstanceMethodAsync(int g, int h, string valString, EvaluateTestsStructWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            await Task.CompletedTask;
        }

        public void InstanceMethod(int g, int h, string valString, EvaluateTestsStructWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
        }

        public void GenericInstanceMethod<T>(int g, int h, string valString, EvaluateTestsStructWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            T t = default(T);
        }

        public async Task<T> GenericInstanceMethodAsync<T>(int g, int h, string valString, EvaluateTestsStructWithProperties me)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            var local_dt = new DateTime(2025, 3, 5, 7, 9, 11);
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
            T t = default(T);
            return await Task.FromResult(default(T));
        }
    }
    public class EvaluateMethodTestsClass
    {
        public class ParmToTest
        {
            public int a;
            public int b;
            public ParmToTest()
            {
                a = 10;
                b = 10;
            }
            public string MyMethod()
            {
                return "methodOK";
            }
        }
        public class TestEvaluate
        {
            public int a;
            public int b;
            public int c;
            public string str = "str_const_";
            public bool t = true;
            public bool f = false;
            public ParmToTest objToTest;
            public ParmToTest ParmToTestObj => objToTest;
            public ParmToTest ParmToTestObjNull => null;
            public ParmToTest ParmToTestObjException => throw new Exception("error2");
            public void run(int g, int h, string a, string valString, int this_a)
            {
                objToTest = new ParmToTest();
                int d = g + 1;
                int e = g + 2;
                int f = g + 3;
                int i = d + e + f;
                this.a = 1;
                b = 2;
                c = 3;
                this.a = this.a + 1;
                b = b + 1;
                c = c + 1;
            }

            public int CallMethod()
            {
                return a;
            }

            public int CallMethodWithParm(int parm)
            {
                return a + parm;
            }

            public void CallMethodChangeValue()
            {
                a = a + 10;
            }

            public int CallMethodWithMultipleParms(int parm, int parm2)
            {
                return a + parm + parm2;
            }

            public string CallMethodWithParmString(string parm)
            {
                return str + parm;
            }

            public string CallMethodWithParmBool(bool parm)
            {
                if (parm)
                    return "TRUE";
                return "FALSE";
            }

            public int CallMethodWithObj(ParmToTest parm)
            {
                if (parm == null)
                    return -1;
                return parm.a;
            }


            public string CallMethodWithChar(char parm)
            {
                return str + parm;
            }
        }

        public static void EvaluateMethods()
        {
            TestEvaluate f = new TestEvaluate();
            f.run(100, 200, "9000", "test", 45);
        }

    }

    public static class EvaluateStaticClass
    {
        public static int StaticField1 = 10;
        public static string StaticProperty1 => "StaticProperty1";
		public static string StaticPropertyWithError => throw new Exception("not implemented");
    }

}
