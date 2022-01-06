// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        public static void EvaluateLocalsFromAnotherAssembly()
        {
            var asm = System.Reflection.Assembly.LoadFrom("debugger-test-with-source-link.dll");
            var myType = asm.GetType("DebuggerTests.ClassToCheckFieldValue");
            var myMethod = myType.GetConstructor(new Type[] { });
            var a = myMethod.Invoke(new object[]{});
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

            public string CallMethodWithParmString_λ(string parm)
            {
                return "λ_" + parm;
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
            DebuggerTestsV2.EvaluateStaticClass.Run();
            var a = 0;
        }

        public static void EvaluateAsyncMethods()
        {
            var staticClass = new EvaluateNonStaticClassWithStaticFields();
            staticClass.run();
        }

    }

    public static class EvaluateStaticClass
    {
        public static int StaticField1 = 10;
        public static string StaticProperty1 => "StaticProperty1";
        public static string StaticPropertyWithError => throw new Exception("not implemented");

        public static class NestedClass1
        {
            public static class NestedClass2
            {
                public static class NestedClass3
                {
                    public static int StaticField1 = 3;
                    public static string StaticProperty1 => "StaticProperty3";
                    public static string StaticPropertyWithError => throw new Exception("not implemented 3");
                }
            }
        }
    }

    public class EvaluateNonStaticClassWithStaticFields
    {
        public static int StaticField1 = 10;
        public static string StaticProperty1 => "StaticProperty1";
        public static string StaticPropertyWithError => throw new Exception("not implemented");

        private int HelperMethod()
        {
            return 5;
        }

        public async void run()
        {
            var makeAwaitable = await Task.Run(() => HelperMethod());
        }
    }

    public class EvaluateLocalsWithElementAccessTests
    {
        public class TestEvaluate
        {
            public List<int> numList;
            public List<string> textList;
            public int[] numArray;
            public string[] textArray;
            public int[][] numArrayOfArrays;
            public List<List<int>> numListOfLists;
            public string[][] textArrayOfArrays;
            public List<List<string>> textListOfLists;
            public int idx0;
            public int idx1;

            public void run()
            {
                numList = new List<int> { 1, 2 };
                textList = new List<string> { "1", "2" };
                numArray = new int[] { 1, 2 };
                textArray = new string[] { "1", "2" };
                numArrayOfArrays = new int[][] { numArray, numArray };
                numListOfLists = new List<List<int>> { numList, numList };
                textArrayOfArrays = new string[][] { textArray, textArray };
                textListOfLists = new List<List<string>> { textList, textList };
                idx0 = 0;
                idx1 = 1;
            }        
        }

        public static void EvaluateLocals()
        {
            int i = 0;
            int j = 1;
            TestEvaluate f = new TestEvaluate();
            f.run();
        }
    }

    public static class EvaluateBrowsableProperties
    {
        public class TestEvaluateFieldsNone
        {
            public List<int> list = new List<int>() { 1, 2 };
            public int[] array = new int[] { 11, 22 };
            public string text = "text";
        }

        public class TestEvaluatePropertiesNone
        {
            public List<int> list { get; set; }
            public int[] array { get; set; }
            public string text { get; set; }
            
            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
            }
        }

        public class TestEvaluateFieldsNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever = "textNever";
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
            }
        }

        public class TestEvaluateFieldsCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed = "textCollapsed";
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
            }
        }

        public class TestEvaluateFieldsRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden = "textRootHidden";
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
            }
        }

        public static void Evaluate()
        {
            var testFieldsNone = new TestEvaluateFieldsNone();
            var testFieldsNever = new TestEvaluateFieldsNever();
            var testFieldsCollapsed = new TestEvaluateFieldsCollapsed();
            var testFieldsRootHidden = new TestEvaluateFieldsRootHidden();

            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateBrowsableStaticProperties
    {
        public class TestEvaluateFieldsNone
        {
            public static List<int> list = new List<int>() { 1, 2 };
            public static int[] array = new int[] { 11, 22 };
            public static string text = "text";
        }

        public class TestEvaluatePropertiesNone
        {
            public static List<int> list { get; set; }
            public static int[] array { get; set; }
            public static string text { get; set; }
            
            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
            }
        }

        public class TestEvaluateFieldsNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever = "textNever";
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
            }
        }

        public class TestEvaluateFieldsCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed = "textCollapsed";
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
            }
        }

        public class TestEvaluateFieldsRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden = "textRootHidden";
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
            }
        }

        public static void Evaluate()
        {
            var testFieldsNone = new TestEvaluateFieldsNone();
            var testFieldsNever = new TestEvaluateFieldsNever();
            var testFieldsCollapsed = new TestEvaluateFieldsCollapsed();
            var testFieldsRootHidden = new TestEvaluateFieldsRootHidden();

            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateBrowsableCustomProperties
    {
        public class TestEvaluatePropertiesNone
        {
            public List<int> list { get { return new List<int>() { 1, 2 }; } }
            public int[] array { get { return new int[] { 11, 22 }; } }
            public string text { get { return "text"; } }
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get { return "textNever"; } }
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get { return "textCollapsed"; } }
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get { return "textRootHidden"; } }
        }

        public static void Evaluate()
        {
            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateProtectionLevels
    {
        public class TestClass
        {
            public string fieldPublic = "public";
            private string fieldPrivate = "private";
            internal string fieldInternal = "internal";
            protected string fieldProtected = "protected";

            public TestClass()
            {
                var a = fieldPrivate;
            }
        }

        public static void Evaluate()
        {
            var testClass = new TestClass();
        }
    }
}

namespace DebuggerTestsV2
{
    public static class EvaluateStaticClass
    {
        public static int StaticField1 = 20;
        public static string StaticProperty1 => "StaticProperty2";
        public static string StaticPropertyWithError => throw new Exception("not implemented");

        public static void Run()
        {
            var a = 0;
        }
    }
}


public static class NoNamespaceClass
{
    public static void EvaluateMethods()
    {
        var stopHere = true;
    }
    
    public static class NestedClass1
    {
        public static class NestedClass2
        {
            public static class NestedClass3
            {
                public static int StaticField1 = 30;
                public static string StaticProperty1 => "StaticProperty30";
                public static string StaticPropertyWithError => throw new Exception("not implemented 30");
            }
        }
    }
}

