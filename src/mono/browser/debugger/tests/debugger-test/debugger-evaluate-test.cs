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
            var asm = System.Reflection.Assembly.LoadFrom("lazy-debugger-test.dll");
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

            public char CallMethodReturningChar()
            {
                return 'A';
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

            public string CallMethodWithParmString_\u03BB(string parm)
            {
                return "\u03BB_" + parm;
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
            DebuggerTestsV2.EvaluateStaticFieldsInStaticClass.Run();
            DebuggerTests.EvaluateStaticFieldsInStaticClass.Run();
            DebuggerTests.EvaluateStaticFieldsInInstanceClass.RunStatic();
            var instanceWithStaticFields = new EvaluateStaticFieldsInInstanceClass();
            instanceWithStaticFields.Run();
        }

        public static async Task EvaluateMethodsAsync()
        {
            await DebuggerTests.EvaluateStaticFieldsInStaticClass.RunAsync();
            await DebuggerTests.EvaluateStaticFieldsInInstanceClass.RunStaticAsync();
            var instanceWithStaticFields = new EvaluateStaticFieldsInInstanceClass();
            await instanceWithStaticFields.RunAsync();
        }
    }

    public static class EvaluateStaticFieldsInStaticClass
    {
        public static int StaticField = 10;
        public static string StaticProperty => "StaticProperty1";
        public static string StaticPropertyWithError => throw new Exception("not implemented 1");

        public static void Run()
        {
            bool stop = true;
        }

        public async static Task RunAsync()
        {
            await Task.FromResult(0);
        }

        public static class NestedClass1
        {
            public static class NestedClass2
            {
                public static class NestedClass3
                {
                    public static int StaticField = 3;
                    public static string StaticProperty => "StaticProperty3";
                    public static string StaticPropertyWithError => throw new Exception("not implemented 3");
                }
            }
        }
    }

    public class EvaluateStaticFieldsInInstanceClass
    {
        public static int StaticField = 70;
        public static string StaticProperty => "StaticProperty7";
        public static string StaticPropertyWithError => throw new Exception("not implemented 7");

        public void Run()
        {
            bool stop = true;
        }

        public async Task RunAsync()
        {
            await Task.FromResult(0);
        }

        public static void RunStatic()
        {
            bool stop = true;
        }

        public static async Task RunStaticAsync()
        {
            await Task.FromResult(0);
        }

        private int HelperMethod()
        {
            return 5;
        }

        public async void run()
        {
            var makeAwaitable = await Task.Run(() => HelperMethod());
        }
    }

    public class EvaluateLocalsWithIndexingTests
    {
        public record Indexer(int index);

        public class CommonCollections
        {
            public List<int> numList = new List<int> { 1, 2 };
            public List<string> textList = new List<string> { "1", "2" };
            public int[] numArray = new int[] { 1, 2, 0 };
            public string[] textArray = new string[] { "1", "2" };
            public int[][] numArrayOfArrays;
            public List<List<int>> numListOfLists;
            public string[][] textArrayOfArrays;
            public List<List<string>> textListOfLists;
            public Dictionary<string, bool> indexedByStr = new Dictionary<string, bool>() { { "1", true }, { "111", false }, { "true", true} };
            public Dictionary<char, string> indexedByChar = new Dictionary<char, string>() { { 'i', "I" }, { '5', "5" } };
            public Dictionary<bool, string> indexedByBool = new Dictionary<bool, string>() { { true, "TRUE" }, { false, "FALSE" } };
            public int idx0 = 0;
            public int idx1 = 1;

            public CommonCollections()
            {
                numArrayOfArrays = new int[][] { numArray, numArray };
                numListOfLists = new List<List<int>> { numList, numList };
                textArrayOfArrays = new string[][] { textArray, textArray };
                textListOfLists = new List<List<string>> { textList, textList };
            }
        }

        public class ClassWithIndexers
        {
            public string this[char keyChar] => "res_" + keyChar;
            public string this[bool keyBool] => keyBool.ToString();
            public bool this[string keyStr] => keyStr.Length > 3;
            public int this[double keyDouble] => (int)keyDouble;
            public int this[float keyFloat] => (int)keyFloat;
            public int this[decimal keyDecimal] => (int)keyDecimal;
            public int this[Indexer indexer] => indexer.index;
            public char this[char[] arr] => arr.Length == 0 ? '0' : arr[0];

            public double this[int key1, double key2] => key1 + key2;
            public string this[char key1, string key2, string key3] => $"{key1}-{key2}-{key3}";

            public InlineArray.Arr1 inlineArr;
        }

        public struct StructWithIndexers
        {
            public string this[char keyChar] => "res_" + keyChar;
            public string this[bool keyBool] => keyBool.ToString();
            public bool this[string keyStr] => keyStr.Length > 3;
            public int this[double keyDouble] => (int)keyDouble;
            public int this[float keyFloat] => (int)keyFloat;
            public int this[decimal keyDecimal] => (int)keyDecimal;
            public int this[Indexer indexer] => indexer.index;
            public char this[char[] arr] => arr.Length == 0 ? '0' : arr[0];

            public double this[int key1, double key2] => key1 + key2;
            public string this[char key1, string key2, string key3] => $"{key1}-{key2}-{key3}";

            public InlineArray.Arr1 inlineArr;
        }

        public static void EvaluateLocals()
        {
            int i = 0;
            int j = 1;
            ClassWithIndexers c = new();
            StructWithIndexers s = new();
            CommonCollections cc = new();
            string longString = "longString";
            string shortString = "9";
            char aChar = '9';
            bool aBool = true;
            float aFloat = 1.23f;
            double aDouble = 2.34;
            decimal aDecimal = 3.34m;
            Indexer objIdx = new(index: 123);
            char[] arr = new char[] { 't', 'e', 's', 't' };
        }
    }

    public class EvaluateLocalsWithMultidimensionalIndexingTests
    {
        public class TestEvaluate
        {
            public int[,] numArray2D;
            public string[,] textArray2D;
            public int[,,] numArray3D;
            public int idx0;
            public int idx1;

            public void run()
            {
                numArray2D = new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } };
                textArray2D = new string[3, 2] { { "one", "two" }, { "three", "four" },
                                            { "five", "six" } };
                numArray3D = new int[,,] { { { 1, 2, 3 }, { 4, 5, 6 } },
                                    { { 7, 8, 9 }, { 10, 11, 12 } } };
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

    public struct SampleStructure
    {
        public SampleStructure() { }

        public int Id = 100;

        internal bool IsStruct = true;
    }

    public enum SampleEnum
    {
        yes = 0,
        no = 1
    }

    public class SampleClass
    {
        public int ClassId = 200;
        public List<string> Items = new List<string> { "should not be expanded" };
    }

    public static class EvaluateBrowsableClass
    {
        public class TestEvaluateFieldsNone
        {
            public List<int> list = new List<int>() { 1, 2 };
            public int[] array = new int[] { 11, 22 };
            public string text = "text";
            public bool[] nullNone = null;
            public SampleEnum valueTypeEnum = new();
            public SampleStructure sampleStruct = new();
            public SampleClass sampleClass = new();
        }

        public class TestEvaluatePropertiesNone
        {
            public List<int> list { get; set; }
            public int[] array { get; set; }
            public string text { get; set; }
            public bool[] nullNone { get; set; }
            public SampleEnum valueTypeEnum { get; set; }
            public SampleStructure sampleStruct { get; set; }
            public SampleClass sampleClass { get; set; }

            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
                nullNone = null;
                valueTypeEnum = new();
                sampleStruct = new();
                sampleClass = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever = new();
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
                nullNever = null;
                valueTypeEnumNever = new();
                sampleStructNever = new();
                sampleClassNever = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed = new();
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
                nullCollapsed = null;
                valueTypeEnumCollapsed = new();
                sampleStructCollapsed = new();
                sampleClassCollapsed = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden = new();
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
                nullRootHidden = null;
                valueTypeEnumRootHidden = new();
                sampleStructRootHidden = new();
                sampleClassRootHidden = new();
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

    public static class EvaluateBrowsableStruct
    {
        public struct TestEvaluateFieldsNone
        {
            public TestEvaluateFieldsNone() {}
            public List<int> list = new List<int>() { 1, 2 };
            public int[] array = new int[] { 11, 22 };
            public string text = "text";
            public bool[] nullNone = null;
            public SampleEnum valueTypeEnum = new();
            public SampleStructure sampleStruct = new();
            public SampleClass sampleClass = new();
        }

        public struct TestEvaluatePropertiesNone
        {
            public List<int> list { get; set; }
            public int[] array { get; set; }
            public string text { get; set; }
            public bool[] nullNone { get; set; }
            public SampleEnum valueTypeEnum { get; set; }
            public SampleStructure sampleStruct { get; set; }
            public SampleClass sampleClass { get; set; }

            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
                nullNone = null;
                valueTypeEnum = new();
                sampleStruct = new();
                sampleClass = new();
            }
        }

        public struct TestEvaluateFieldsNever
        {
            public TestEvaluateFieldsNever() {}

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever = "textNever";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever = new();
        }

        public struct TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
                nullNever = null;
                valueTypeEnumNever = new();
                sampleStructNever = new();
                sampleClassNever = new();
            }
        }

        public struct TestEvaluateFieldsCollapsed
        {
            public TestEvaluateFieldsCollapsed() {}

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed = "textCollapsed";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed = new();
        }

        public struct TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
                nullCollapsed = null;
                valueTypeEnumCollapsed = new();
                sampleStructCollapsed = new();
                sampleClassCollapsed = new();
            }
        }

        public struct TestEvaluateFieldsRootHidden
        {
            public TestEvaluateFieldsRootHidden() {}

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden = "textRootHidden";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden = new();
        }

        public struct TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
                nullRootHidden = null;
                valueTypeEnumRootHidden = new();
                sampleStructRootHidden = new();
                sampleClassRootHidden = new();
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

    public static class EvaluateBrowsableClassStatic
    {
        public class TestEvaluateFieldsNone
        {
            public static List<int> list = new List<int>() { 1, 2 };
            public static int[] array = new int[] { 11, 22 };
            public static string text = "text";

            public static bool[] nullNone = null;
            public static SampleEnum valueTypeEnum = new();
            public static SampleStructure sampleStruct = new();
            public static SampleClass sampleClass = new();
        }

        public class TestEvaluatePropertiesNone
        {
            public static List<int> list { get; set; }
            public static int[] array { get; set; }
            public static string text { get; set; }
            public static bool[] nullNone { get; set; }
            public static SampleEnum valueTypeEnum { get; set; }
            public static SampleStructure sampleStruct { get; set; }
            public static SampleClass sampleClass { get; set; }

            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
                nullNone = null;
                valueTypeEnum = new();
                sampleStruct = new();
                sampleClass = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever = new();
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
                nullNever = null;
                valueTypeEnumNever = new();
                sampleStructNever = new();
                sampleClassNever = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleEnum valueTypeEnumCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleStructure sampleStructCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed = new();
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleEnum valueTypeEnumCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleStructure sampleStructCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
                nullCollapsed = null;
                valueTypeEnumCollapsed = new();
                sampleStructCollapsed = new();
                sampleClassCollapsed = new();
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

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden = new();
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
                nullRootHidden = null;
                valueTypeEnumRootHidden = new();
                sampleStructRootHidden = new();
                sampleClassRootHidden = new();
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

    public static class EvaluateBrowsableStructStatic
    {
        public struct TestEvaluateFieldsNone
        {

            public static List<int> list = new List<int>() { 1, 2 };
            public static int[] array = new int[] { 11, 22 };
            public static string text = "text";

            public static bool[] nullNone = null;
            public static SampleEnum valueTypeEnum = new();
            public static SampleStructure sampleStruct = new();
            public static SampleClass sampleClass = new();

            public TestEvaluateFieldsNone() { }
        }

        public struct TestEvaluatePropertiesNone
        {
            public static List<int> list { get; set; }
            public static int[] array { get; set; }
            public static string text { get; set; }
            public static bool[] nullNone { get; set; }
            public static SampleEnum valueTypeEnum { get; set; }
            public static SampleStructure sampleStruct { get; set; }
            public static SampleClass sampleClass { get; set; }

            public TestEvaluatePropertiesNone()
            {
                list = new List<int>() { 1, 2 };
                array = new int[] { 11, 22 };
                text = "text";
                nullNone = null;
                valueTypeEnum = new();
                sampleStruct = new();
                sampleClass = new();
            }
        }

        public struct TestEvaluateFieldsNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever = "textNever";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever = new();
            public TestEvaluateFieldsNever() { }
        }

        public struct TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever { get; set; }

            public TestEvaluatePropertiesNever()
            {
                listNever = new List<int>() { 1, 2 };
                arrayNever = new int[] { 11, 22 };
                textNever = "textNever";
                nullNever = null;
                valueTypeEnumNever = new();
                sampleStructNever = new();
                sampleClassNever = new();
            }
        }

        public struct TestEvaluateFieldsCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed = "textCollapsed";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleEnum valueTypeEnumCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleStructure sampleStructCollapsed = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed = new();
            public TestEvaluateFieldsCollapsed() { }
        }

        public struct TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleEnum valueTypeEnumCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleStructure sampleStructCollapsed { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed { get; set; }

            public TestEvaluatePropertiesCollapsed()
            {
                listCollapsed = new List<int>() { 1, 2 };
                arrayCollapsed = new int[] { 11, 22 };
                textCollapsed = "textCollapsed";
                nullCollapsed = null;
                valueTypeEnumCollapsed = new();
                sampleStructCollapsed = new();
                sampleClassCollapsed = new();
            }
        }

        public struct TestEvaluateFieldsRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden = new List<int>() { 1, 2 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden = new int[] { 11, 22 };

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden = "textRootHidden";

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden = null;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden = new();

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden = new();

            public TestEvaluateFieldsRootHidden() { }
        }

        public struct TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden { get; set; }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden { get; set; }

            public TestEvaluatePropertiesRootHidden()
            {
                listRootHidden = new List<int>() { 1, 2 };
                arrayRootHidden = new int[] { 11, 22 };
                textRootHidden = "textRootHidden";
                nullRootHidden = null;
                valueTypeEnumRootHidden = new();
                sampleStructRootHidden = new();
                sampleClassRootHidden = new();
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

    public static class EvaluateBrowsableNonAutoPropertiesClass
    {
        public class TestEvaluatePropertiesNone
        {
            public List<int> list { get { return new List<int>() { 1, 2 }; } }
            public int[] array { get { return new int[] { 11, 22 }; } }
            public string text { get { return "text"; } }
            public bool[] nullNone { get { return null; } }
            public SampleEnum valueTypeEnum { get { return new(); } }
            public SampleStructure sampleStruct { get { return new(); } }
            public SampleClass sampleClass { get { return new(); } }
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get { return "textNever"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever { get { return new(); } }
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get { return "textCollapsed"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed { get { return new(); } }
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get { return "textRootHidden"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden { get { return new(); } }
        }

        public static void Evaluate()
        {
            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateBrowsableNonAutoPropertiesStruct
    {
        public struct TestEvaluatePropertiesNone
        {
            public List<int> list { get { return new List<int>() { 1, 2 }; } }
            public int[] array { get { return new int[] { 11, 22 }; } }
            public string text { get { return "text"; } }
            public bool[] nullNone { get { return null; } }
            public SampleEnum valueTypeEnum { get { return new(); } }
            public SampleStructure sampleStruct { get { return new(); } }
            public SampleClass sampleClass { get { return new(); } }
        }

        public struct TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public List<int> listNever { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int[] arrayNever { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public string textNever { get { return "textNever"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool[] nullNever { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleEnum valueTypeEnumNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleStructure sampleStructNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public SampleClass sampleClassNever { get { return new(); } }
        }

        public struct TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public List<int> listCollapsed { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public int[] arrayCollapsed { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public string textCollapsed { get { return "textCollapsed"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public bool[] nullCollapsed { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleClass sampleClassCollapsed { get { return new(); } }
        }

        public struct TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public List<int> listRootHidden { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public int[] arrayRootHidden { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public string textRootHidden { get { return "textRootHidden"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public bool[] nullRootHidden { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleEnum valueTypeEnumRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleStructure sampleStructRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public SampleClass sampleClassRootHidden { get { return new(); } }
        }

        public static void Evaluate()
        {
            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateBrowsableNonAutoPropertiesClassStatic
    {
        public class TestEvaluatePropertiesNone
        {
            public static List<int> list { get { return new List<int>() { 1, 2 }; } }
            public static int[] array { get { return new int[] { 11, 22 }; } }
            public static string text { get { return "text"; } }
            public static bool[] nullNone { get { return null; } }
            public static SampleEnum valueTypeEnum { get { return new(); } }
            public static SampleStructure sampleStruct { get { return new(); } }
            public static SampleClass sampleClass { get { return new(); } }
        }

        public class TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever { get { return "textNever"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever { get { return new(); } }
        }

        public class TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed { get { return "textCollapsed"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleEnum valueTypeEnumCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleStructure sampleStructCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed { get { return new(); } }
        }

        public class TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden { get { return "textRootHidden"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden { get { return new(); } }
        }

        public static void Evaluate()
        {
            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class EvaluateBrowsableNonAutoPropertiesStructStatic
    {
        public struct TestEvaluatePropertiesNone
        {
            public static List<int> list { get { return new List<int>() { 1, 2 }; } }
            public static int[] array { get { return new int[] { 11, 22 }; } }
            public static string text { get { return "text"; } }
            public static bool[] nullNone { get { return null; } }
            public static SampleEnum valueTypeEnum { get { return new(); } }
            public static SampleStructure sampleStruct { get { return new(); } }
            public static SampleClass sampleClass { get { return new(); } }
        }

        public struct TestEvaluatePropertiesNever
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static List<int> listNever { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static int[] arrayNever { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static string textNever { get { return "textNever"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static bool[] nullNever { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleEnum valueTypeEnumNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleStructure sampleStructNever { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static SampleClass sampleClassNever { get { return new(); } }
        }

        public struct TestEvaluatePropertiesCollapsed
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static List<int> listCollapsed { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static int[] arrayCollapsed { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static string textCollapsed { get { return "textCollapsed"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static bool[] nullCollapsed { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleEnum valueTypeEnumCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public SampleStructure sampleStructCollapsed { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
            public static SampleClass sampleClassCollapsed { get { return new(); } }
        }

        public struct TestEvaluatePropertiesRootHidden
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static List<int> listRootHidden { get { return new List<int>() { 1, 2 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static int[] arrayRootHidden { get { return new int[] { 11, 22 }; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static string textRootHidden { get { return "textRootHidden"; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static bool[] nullRootHidden { get { return null; } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleEnum valueTypeEnumRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleStructure sampleStructRootHidden { get { return new(); } }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public static SampleClass sampleClassRootHidden { get { return new(); } }
        }

        public static void Evaluate()
        {
            var testPropertiesNone = new TestEvaluatePropertiesNone();
            var testPropertiesNever = new TestEvaluatePropertiesNever();
            var testPropertiesCollapsed = new TestEvaluatePropertiesCollapsed();
            var testPropertiesRootHidden = new TestEvaluatePropertiesRootHidden();
        }
    }

    public static class StructureGetters
    {
        public struct Point
        {
            public int Id { get { return 123; } }
        }

        public static void Evaluate()
        {
            var s = new Point();
        }
    }

    public static class FastCheck
    {
        public class InstanceClass
        {
            public int number = 123;
        }

        public class MemberClass
        {
            public int Method(InstanceClass ic) => ic.number;
            public int Method(int num) => num;
        }

        public static void run()
        {
            int number = -123;
            InstanceClass ic = new();
            MemberClass mc = new();
            EvaluateStaticFieldsInInstanceClass instance = new();
            PrimitiveTypeMethods.TestClass instance2 = new();
        }
    }

    public static class DefaultParamMethods
    {
        public class TestClass
        {
            public IEnumerable<int> listToLinq = System.Linq.Enumerable.Range(1, 11);

            public bool GetBool(bool param = true) => param;
            public char GetChar(char param = 'T') => param;
            public char GetUnicodeChar(char param = '\u0105') => param;
            public byte GetByte(byte param = 1) => param;
            public sbyte GetSByte(sbyte param = 1) => param;
            public short GetInt16(short param = 1) => param;
            public ushort GetUInt16(ushort param = 1) => param;
            public int GetInt32(int param = 1) => param;
            public uint GetUInt32(uint param = 1) => param;
            public long GetInt64(long param = 1) => param;
            public ulong GetUInt64(ulong param = 1) => param;
            public float GetSingle(float param = 1.23f) => param;
            public double GetDouble(double param = 1.23) => param;
            public string GetString(string param = "1.23") => param;
            public string GetUnicodeString(string param = "\u017C\u00F3\u0142\u0107") => param;

#nullable enable
            public bool? GetBoolNullable(bool? param = true) => param;
            public char? GetCharNullable(char? param = 'T') => param;
            public byte? GetByteNullable(byte? param = 1) => param;
            public sbyte? GetSByteNullable(sbyte? param = 1) => param;
            public short? GetInt16Nullable(short? param = 1) => param;
            public ushort? GetUInt16Nullable(ushort? param = 1) => param;
            public int? GetInt32Nullable(int? param = 1) => param;
            public uint? GetUInt32Nullable(uint? param = 1) => param;
            public long? GetInt64Nullable(long? param = 1) => param;
            public ulong? GetUInt64Nullable(ulong? param = 1) => param;
            public float? GetSingleNullable(float? param = 1.23f) => param;
            public double? GetDoubleNullable(double? param = 1.23) => param;
            public string? GetStringNullable(string? param = "1.23") => param;
#nullable disable

            public bool GetNull(object param = null) => param == null ? true : false;
            public int SumDefaultAndRequiredParam(int requiredParam, int optionalParam = 3) => requiredParam + optionalParam;
            public int SumDefaultNegativeAndRequiredParamInts(int requiredParam, int optionalParam = -3) => requiredParam + optionalParam;
            public long SumDefaultNegativeAndRequiredParamLongInts(long requiredParam, long optionalParam = -123) => requiredParam + optionalParam;
            public float SumDefaultNegativeAndRequiredParamFloats(float requiredParam, float optionalParam = -3.3f) => requiredParam + optionalParam;
            public double SumDefaultNegativeAndRequiredParamDoubles(double requiredParam, double optionalParam = -3.2) => requiredParam + optionalParam;
            public short SumDefaultNegativeAndRequiredParamShortInts(short requiredParam, short optionalParam = -123) => (short)(requiredParam + optionalParam);
            public short GetDefaultNegativeShortInt(short optionalParam = -123) => optionalParam;
            public string GetDefaultAndRequiredParamMixedTypes(string requiredParam, int optionalParamFirst = -1, bool optionalParamSecond = false) => $"{requiredParam}; {optionalParamFirst}; {optionalParamSecond}";
        }

        public static void Evaluate()
        {
            var test = new TestClass();
        }
    }

    public static class PrimitiveTypeMethods
    {
        public class TestClass
        {
            public int propInt = 12;
            public uint propUint = 12;
            public long propLong = 12;
            public ulong propUlong = 12;
            public float propFloat = 1.2345678f;
            public double propDouble = 1.2345678910111213;
            public bool propBool = true;
            public char propChar = 'X';
            public string propString = "s_t_r";
        }

        public static void Evaluate()
        {
            var test = new TestClass();
            int localInt = 2;
            uint localUint = 2;
            long localLong = 2;
            ulong localUlong = 2;
            float localFloat = 0.2345678f;
            double localDouble = 0.2345678910111213;
            bool localBool = false;
            char localChar = 'Y';
            string localString = "S*T*R";
        }
    }

    public static class EvaluateNullableProperties
    {
        class TestClass
        {
            public List<int> MemberListNull = null;
            public List<int> MemberList = new List<int>() {1, 2};
            public TestClass Sibling { get; set; }
        }
        static void Evaluate()
        {
            #nullable enable
            List<int>? listNull = null;
            #nullable disable
            List<int> list = new List<int>() {1};
            TestClass tc = new TestClass();
            TestClass tcNull = null;
            string str = "str#value";
            string str_null = null;
            int x = 5;
            int? x_null = null;
            int? x_val = x;
        }
    }

    public static class TypeProperties
    {
        public class InstanceProperties
        {
            public string str = "aB.c[";
        }

        public static void Run()
        {
            var instance = new InstanceProperties();
            var localString = "aB.c[";
        }
    }

    public static class EvaluateMethodsOnEnum
    {
        public static SampleEnum s_valueTypeEnum = SampleEnum.no;
        public class MemberClass
        {
            public SampleEnum valueTypeEnum = SampleEnum.yes;
        }
        public static void run()
        {
            MemberClass mc = new();
            Console.WriteLine("Break here");
        }
    }
}

namespace DebuggerTestsV2
{
    public static class EvaluateStaticFieldsInStaticClass
    {
        public static int StaticField = 20;
        public static string StaticProperty => "StaticProperty2";
        public static string StaticPropertyWithError => throw new Exception("not implemented 2");

        public static void Run()
        {
            var a = 0;
        }
    }
}

public static class NestedWithSameNames
{
    public static int StaticField = 30;
    public static string StaticProperty => "StaticProperty3";
    public static string StaticPropertyWithError => throw new Exception("not implemented V3");

    public static class B
    {
        public static int StaticField = 60;
        public static string StaticProperty => "StaticProperty6";
        public static string StaticPropertyWithError => throw new Exception("not implemented V6");

        public static class NestedWithSameNames
        {
            public static class B
            {
                public static int NestedWithSameNames = 90;
                public static int StaticField = 40;
                public static string StaticProperty => "StaticProperty4";
                public static string StaticPropertyWithError => throw new Exception("not implemented V4");

                public static void Run()
                {
                    var a = 0;
                }
            }
        }
        public static class NestedWithDifferentName
        {
            public static class B
            {
                public static int StaticField = 70;
                public static string StaticProperty => "StaticProperty7";
                public static string StaticPropertyWithError => throw new Exception("not implemented V7");
            }
        }
    }

    public static void Evaluate()
    {
        B.NestedWithSameNames.B.Run();
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
                public static int StaticField = 30;
                public static string StaticProperty => "StaticProperty30";
                public static string StaticPropertyWithError => throw new Exception("not implemented 30");
            }
        }
    }
}
[System.Diagnostics.DebuggerDisplay("{MyMethod2(),nq}")]
public class TestEvaluateDontPauseOnBreakpoint
{
    public int count;
    public static void run()
    {
        var myVar = new TestEvaluateDontPauseOnBreakpoint();
        myVar.count = 10;
        myVar.MyMethod2();
        myVar.MyMethod();
    }
    public string MyMethod() {
        System.Diagnostics.Debugger.Break();
        return string.Format("Object {0}", count);
    }
    public string MyMethod2() {
        return string.Format("Object {0}", count + 1);
    }
    public string MyMethod3() {
        return MyMethod2();
    }
    public string MyCount
    {
        get
        {
            return MyMethod2();
        }
    }
}
public struct EvaluateStaticGetterInValueType
{
    public static int A => 5;
}

namespace DebuggerTests
{
    public class SumObjectAndString
    {
        public class MyClass
        {
            public override string ToString()
            {
                return "OverridenToString";
            }
        }
        public static void run()
        {
            DateTime dt = new DateTime();
            List<int> myList = new();
            List<int> listNull = null;
            object o = new();
            MyClass myClass = new();
            myList.Add(1);
            Console.WriteLine(myList);
            Console.WriteLine(dt);
        }
    }
    public unsafe struct EvaluateValueTypeWithFixedArray
    {
        private fixed int myIntArray[4];
        private fixed char myCharArray[3];
        double myDouble;
        public EvaluateValueTypeWithFixedArray()
        {
            myDouble = 10;
            myIntArray[0] = 1;
            myIntArray[1] = 2;
            myCharArray[2] = 'a';
        }
        public int MyMethod()
        {
            Console.WriteLine(myDouble);
            return myIntArray[0] + myIntArray[1] + (int)myDouble;
        }
        public static void run()
        {
            var myVar = new EvaluateValueTypeWithFixedArray();
            Console.WriteLine("pause here");
            myVar.MyMethod();
        }
    }

    public struct EvaluateValueTypeWithObjectValueType
    {
        private object myObject;
        double myDouble;
        public EvaluateValueTypeWithObjectValueType()
        {
            myObject = new int();
            myDouble = 10;
        }
        public int MyMethod()
        {
            Console.WriteLine(myDouble);
            return (int)myObject + (int)myDouble;
        }
        public static void run()
        {
            var myVar = new EvaluateValueTypeWithObjectValueType();
            Console.WriteLine("pause here");
            myVar.MyMethod();
        }
    }
}