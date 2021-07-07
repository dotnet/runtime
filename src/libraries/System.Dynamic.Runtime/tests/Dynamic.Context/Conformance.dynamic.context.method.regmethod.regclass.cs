// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;
using System;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth
{
    public class MyClass
    {
        public int Field = 0;
    }

    public struct MyStruct
    {
        public int Number;
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    public class MemberClass
    {
        public bool Method_ReturnBool() => false;

        public bool Method_ReturnBool(object o) => o == null;

        public bool Method_ReturnBool(out byte? p1, ref ulong? p2, out int p3)
        {
            p1 = 3;
            p2 = null;
            p3 = 1;
            return false;
        }

        public bool Method_ReturnBool(params object[] o) => false;

        public bool? Method_ReturnBoolNullable() => false;

        public bool? Method_ReturnBoolNullable(MyEnum[] p1, out decimal?[] p2, params int?[] p3)
        {
            p2 = new decimal?[2];
            return (bool?)true;
        }

        public bool? Method_ReturnBoolNullable(params short?[] s) => false;

        public bool? Method_ReturnBoolNullable(short? s)
        {
            return s == null ? null : (bool?)true;
        }

        public byte?[] Method_ReturnByteArrNullable()
        {
            byte?[] arr = new byte?[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = i;
            return arr;
        }

        public byte?[] Method_ReturnByteArrNullable(MyStruct? m)
        {
            byte?[] arr = new byte?[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = (byte)m.Value.Number;
            return arr;
        }

        public byte?[] Method_ReturnByteArrNullable(out MyStruct? m)
        {
            m = new MyStruct() { Number = 2 };
            byte?[] arr = new byte?[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = (byte)m.Value.Number;
            return arr;
        }

        public byte?[] Method_ReturnByteArrNullable(out string p1, ref ulong? p2, ref byte?[] p3)
        {
            p1 = "one";
            p2 = null;
            byte?[] arr = new byte?[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = 2;
            return arr;
        }

        public byte?[] Method_ReturnByteArrNullable(params MyStruct?[] m)
        {
            byte?[] arr = new byte?[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = i;
            return arr;
        }

        public byte[] Method_ReturnByteArr()
        {
            byte[] arr = new byte[10];
            for (byte i = 0; i < 10; i++)
                arr[i] = i;
            return arr;
        }

        public byte[] Method_ReturnByteArr(MyClass[] d)
        {
            byte[] arr = new byte[d.Length];
            for (byte i = 0; i < d.Length; i++)
                arr[i] = 5;
            return arr;
        }

        public byte[] Method_ReturnByteArr(out short?[] p1, ref bool?[] p2, ref MyEnum p3)
        {
            p1 = new short?[] { 1, null };
            p2[0] = null;
            p3 = MyEnum.Third;
            byte[] arr = new byte[2];
            for (byte i = 0; i < 2; i++)
                arr[i] = (byte)(p1[i] ?? 0);
            return arr;
        }

        public byte[] Method_ReturnByteArr(params int[] arr)
        {
            byte[] arr2 = new byte[arr.Length];
            for (byte i = 0; i < arr.Length; i++)
                arr2[i] = (byte)arr[i];
            return arr2;
        }

        public char Method_ReturnChar(MyStruct m) => 's';

        public char Method_ReturnChar(params MyStruct[] m) => 'b';

        public char Method_ReturnChar(ref MyStruct m)
        {
            m.Number = 2;
            return '2';
        }

        public char Method_ReturnChar(ref string p1, object p2, ulong[] p3)
        {
            p1 = p1.ToLower();
            return 'c';
        }

        public char? Method_ReturnCharNullable() => 'a';

        public char? Method_ReturnCharNullable(decimal[] p1, ref object p2, ref char p3)
        {
            p2 = new MyClass() { Field = 3 };
            p3 = 'b';
            return p3;
        }

        public char? Method_ReturnCharNullable(MyClass m) => m.ToString()[0];

        public char? Method_ReturnCharNullable(out MyClass m)
        {
            m = new MyClass() { Field = 5 };
            return m.ToString()[0];
        }

        public char? Method_ReturnCharNullable(params MyClass[] m) => 'z';

        public decimal Method_ReturnDecimal() => 1M;

        public decimal Method_ReturnDecimal(int? i) => (decimal)i;

        public decimal Method_ReturnDecimal(out ulong p1, ref MyEnum p2, params char?[] p3)
        {
            p1 = (ulong)p3.Length;
            p2 = MyEnum.Third;
            return (decimal)1;
        }

        public decimal Method_ReturnDecimal(params int?[] i) => 1M;

        public decimal? Method_ReturnDecimalNullable() => 1M;

        public decimal? Method_ReturnDecimalNullable(int i) => i;

        public decimal? Method_ReturnDecimalNullable(params int[] i) => 1M;

        public decimal? Method_ReturnDecimalNullable(ref int? p1, ref short? p2, out MyStruct[] p3)
        {
            p1 = null;
            p2 = null;
            p3 = new MyStruct[2];
            return 3m;
        }

        public dynamic Method_ReturnDynamic() => 3;

        public dynamic Method_ReturnDynamic(ref int x) => x;

        public dynamic Method_ReturnDynamic(out float x)
        {
            x = 3.5f;
            return x;
        }

        public dynamic Method_ReturnDynamic(params dynamic[] d) => d.Length;

        public dynamic Method_ReturnDynamic(dynamic d, int x, ref dynamic e) => x;

        public float Method_ReturnFloat() => 3.4534f;

        public float Method_ReturnFloat(int x) => x;

        public float Method_ReturnFloat(params float[] p1) => p1[0];

        public float Method_ReturnFloat(ref float p1, out float p2, params float[] p3)
        {
            p1 = 0.000003f;
            p2 = 2342424;
            return p3.Length;
        }

        public float? Method_ReturnFloatNullable() => 3.4534f;

        public float? Method_ReturnFloatNullable(int x) => x;

        public float? Method_ReturnFloatNullable(params float[] p1) => p1[0];

        public float? Method_ReturnFloatNullable(ref float? p1, out float p2, params float?[] p3)
        {
            p1 = 0.000003f;
            p2 = 2342424;
            return p3.Length;
        }

        public float?[] Method_ReturnFloatArrNullable() => new float?[] { 3.4534f };

        public float?[] Method_ReturnFloatArrNullable(int x) => new float?[] { x };

        public float?[] Method_ReturnFloatArrNullable(params float[] p1) => new float?[] { p1[0] };

        public float?[] Method_ReturnFloatArrNullable(ref float p1, out float p2, params float[] p3)
        {
            p1 = 0.000003f;
            p2 = 2342424;
            return new float?[] { p3.Length };
        }

        public float[] Method_ReturnFloatArr() => new float[] { 3.4534f };

        public float[] Method_ReturnFloatArr(int x) => new float[] { x };

        public float[] Method_ReturnFloatArr(params float[] p1) => new float[] { p1[0] };

        public float[] Method_ReturnFloatArr(ref float p1, out float p2, params float[] p3)
        {
            p1 = 0.000003f;
            p2 = 2342424;
            return new float[] { p3.Length };
        }

        public int Method_ReturnInt(decimal[] p1, char[] p2, byte p3) => 5;

        public int Method_ReturnInt(params string[] s) => 1;

        public int Method_ReturnInt(ref string s)
        {
            s = s.ToUpper();
            return s.Length;
        }

        public int Method_ReturnInt(string s) => s.Length;

        public int? Method_ReturnIntNullable() => 1;

        public int? Method_ReturnIntNullable(bool b) => null;

        public int? Method_ReturnIntNullable(out MyClass p1, ref decimal p2, char p3)
        {
            p1 = new MyClass() { Field = 3 };
            p2 = 3m;
            return p3;
        }

        public int? Method_ReturnIntNullable(params bool[] b) => 1;

        public int? Method_ReturnIntNullable(ref bool b)
        {
            b = true;
            return b == true ? (int?)1 : null;
        }

        public MyClass Method_ReturnMyClass() => new MyClass() { Field = 1 };

        public MyClass Method_ReturnMyClass(out int? p1, short? p2, out byte p3)
        {
            p1 = 2;
            p3 = 3;
            return new MyClass { Field = (int)p2 };
        }

        public MyClass Method_ReturnMyClass(out ulong l)
        {
            l = 3;
            return  new MyClass { Field = (int)l };
        }

        public MyClass Method_ReturnMyClass(params MyStruct[] arr) => new MyClass { Field = 1 };

        public MyClass Method_ReturnMyClass(ulong l) => new MyClass { Field = (int)l };

        public MyClass Method_ReturnMyClass_Throw()
        {
            throw new ArithmeticException("Test exception");
        }

        public MyClass Method_ReturnMyClass_Throw(bool? b)
        {
            throw new ArgumentException("Test exception", nameof(b));
        }

        public MyClass Method_ReturnMyClass_Throw(out MyClass p1, out decimal?[] p2, MyEnum p3)
        {
            throw new ArgumentException("Test exception", "b");
        }

        public MyClass Method_ReturnMyClass_Throw(params byte?[] arr)
        {
            throw new ArithmeticException("Test exception");
        }

        public MyEnum Method_ReturnMyEnum() => MyEnum.First;

        public MyEnum Method_ReturnMyEnum(out bool[] p1, int[] p2, out bool? p3)
        {
            p1 = new bool[] { true, false };
            p3 = null;
            return MyEnum.First;
        }

        public MyEnum Method_ReturnMyEnum(params short[] s) => MyEnum.Second;

        public MyEnum Method_ReturnMyEnum(short s) => (MyEnum)s;

        public MyEnum? Method_ReturnMyEnumNullable() => MyEnum.First;

        public MyEnum? Method_ReturnMyEnumNullable(int[] arr) => MyEnum.Second;

        public MyEnum? Method_ReturnMyEnumNullable(params MyClass[] d) => MyEnum.Third;

        public MyEnum? Method_ReturnMyEnumNullable(ref bool[] p1, bool p2, out short[] p3)
        {
            p1 = new bool[2];
            p3 = new short[3];
            return MyEnum.First;
        }

        public MyStruct Method_ReturnMyStruct() => new MyStruct() { Number = 3 };

        public MyStruct Method_ReturnMyStruct(params string[] s) => new MyStruct() { Number = 3 };

        public MyStruct Method_ReturnMyStruct(short p1, out bool p2, params MyClass[] p3)
        {
            p2 = false;
            return new MyStruct() { Number = p3.Length };
        }

        public MyStruct Method_ReturnMyStruct(string s) => new MyStruct() { Number = s.Length };

        public MyStruct? Method_ReturnMyStructNullable() => new MyStruct() { Number = 3 };

        public MyStruct? Method_ReturnMyStructNullable(object o) => new MyStruct() { Number = 3 };

        public MyStruct? Method_ReturnMyStructNullable(out byte? p1, out string[] p2, params ulong[] p3)
        {
            p1 = 3;
            p2 = new string[3];
            return new MyStruct() { Number = p3.Length };
        }

        public MyStruct? Method_ReturnMyStructNullable(params object[] o) => new MyStruct() { Number = o.Length };

        public object Method_ReturnObject() => null;

        public object Method_ReturnObject(byte?[] arr) => null;

        public object Method_ReturnObject(MyEnum[] p1, int p2, ref char?[] p3)
        {
            p3 = new char?[] { 'a', 'b' };
            return null;
        }

        public object Method_ReturnObject(params bool?[] b) => null;

        public short Method_ReturnShort_Throw()
        {
            throw new ArithmeticException("Test message");
        }

        public short Method_ReturnShort_Throw(MyEnum m)
        {
            throw new ArgumentException("Test message", nameof(m));
        }

        public short Method_ReturnShort_Throw(out char? p1, ref string[] p2, short[] p3)
        {
            throw new ArgumentException("Test message", "m");
        }

        public short Method_ReturnShort_Throw(out MyEnum m)
        {
            m = MyEnum.Second;
            throw new ArgumentException("Test message", nameof(m));
        }

        public short Method_ReturnShort_Throw(params MyEnum[] m)
        {
            throw new ArithmeticException("Test message");
        }

        public short? Method_ReturnShort_ThrowNullable()
        {
            throw new ArithmeticException("Test message");
        }

        public short? Method_ReturnShort_ThrowNullable(decimal? d)
        {
            throw new ArgumentException("Test message", nameof(d));
        }

        public short? Method_ReturnShort_ThrowNullable(out char? p1, int[] p2, params bool?[] p3)
        {
            throw new ArgumentException("Test message", "d");
        }

        public short? Method_ReturnShort_ThrowNullable(params decimal?[] d)
        {
            throw new ArithmeticException("Test message");
        }

        public short? Method_ReturnShort_ThrowNullable(ref decimal? d)
        {
            d = 3m;
            throw new ArgumentException("Test message", nameof(d));
        }

        public sbyte Method_ReturnSbyte(int x, dynamic d, short s, decimal dd, MyClass c, float f, short? ss) => 1;

        public string Method_ReturnString() => "string";

        public string Method_ReturnString(char c) => c.ToString();

        public string Method_ReturnString(out ulong?[] p1, out byte[] p2, out int?[] p3)
        {
            p1 = new ulong?[2];
            p2 = new byte[2];
            p3 = new int?[3];
            return "c";
        }

        public string Method_ReturnString(params char[] c) => "string";

        public string[] Method_ReturnStringArr()
        {
            string[] arr = new string[10];
            for (int i = 0; i < 10; i++)
                arr[i] = i.ToString();
            return arr;
        }

        public string[] Method_ReturnStringArr(MyEnum? m)
        {
            string[] arr = new string[10];
            for (int i = 0; i < 10; i++)
                arr[i] = i.ToString();
            return arr;
        }

        public string[] Method_ReturnStringArr(params MyEnum?[] m)
        {
            string[] arr = new string[10];
            for (int i = 0; i < 10; i++)
                arr[i] = i.ToString();
            return arr;
        }

        public string[] Method_ReturnStringArr(ref MyEnum? m)
        {
            string[] arr = new string[10];
            for (int i = 0; i < 10; i++)
                arr[i] = m.ToString();
            m = null;
            return arr;
        }

        public string[] Method_ReturnStringArr(short?[] p1, ref MyStruct p2, out MyStruct[] p3)
        {
            p2 = new MyStruct() { Number = 3 };
            p3 = new MyStruct[2];
            p3[0].Number = 1;
            p3[1].Number = 2;
            string[] arr = new string[10];
            for (int i = 0; i < 10; i++)
                arr[i] = p2.ToString();
            return arr;
        }

        public ulong Method_ReturnUlong() => 1L;

        public ulong Method_ReturnUlong(MyClass m) => (ulong)m.ToString().Length;

        public ulong Method_ReturnUlong(out MyClass m)
        {
            m = new MyClass();
            return (ulong)m.ToString().Length;
        }

        public ulong Method_ReturnUlong(params MyClass[] m) => 1L;

        public ulong Method_ReturnUlong(ref ulong?[] p1, out MyStruct p2, ref byte?[] p3)
        {
            p1[0] = 3;
            p2 = new MyStruct() { Number = 3 };
            p3[0] = 1;
            return 1;
        }

        public ulong? Method_ReturnUlongNullable() => 1L;

        public ulong? Method_ReturnUlongNullable(long c)
        {
            return c == 0 ? (ulong?)1L : null;
        }

        public ulong? Method_ReturnUlongNullable(out object[] p1, out byte[] p2, out decimal? p3)
        {
            p1 = new object[2];
            p2 = new byte[2];
            p3 = 4m;
            return 1L;
        }

        public ulong? Method_ReturnUlongNullable(params char?[] c) => 1L;

        public void Method_ReturnVoid() { }

        public void Method_ReturnVoid(MyStruct[] arr) { }

        public void Method_ReturnVoid(params ulong[] l) { }

        public void Method_ReturnVoid(ref object[] p1, out bool?[] p2, ref MyClass[] p3)
        {
            p1 = null;
            p3 = null;
            p2 = new bool?[1];
            p2[0] = false;
            return;
        }

        public static bool StaticMethod_ReturnBool() => false;
    }
}

namespace System.Dynamic.Runtime.Tests
{
    public class Test
    {
        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass001.regclass001
        [Fact]
        public static void CalledFrom_StaticMethdBody()
        {
            dynamic mc = new MemberClass();
            Assert.False((bool)mc.Method_ReturnBool(new object(), null));
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass002.regclass002
        [Fact]
        public static void CalledFrom_ExtensionMethodBody()
        {
            Test t = new Test();
            Assert.False(t.TestMethod());
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass003.regclass003
        [Fact]
        public static void CalledFrom_MemberInitializerOfObjectInitializer()
        {
            dynamic dy = new MemberClass();
            TestClass tc = new TestClass
            {
                ms = dy.Method_ReturnMyStruct(),
                BoolProperty = dy.Method_ReturnBoolNullable((short?)3),
                ByteArray = dy.Method_ReturnByteArr(0, 1, 2, 3, 4)
            };

            Assert.Equal(3, tc.ms.Number);
            Assert.True(tc.BoolProperty);
            Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, tc.ByteArray);
        }

        public class TestClass
        {
            public MyStruct ms;
            public bool? BoolProperty { get; set; }

            public byte[] ByteArray;
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass004.regclass004
        [Fact]
        public static void CalledFrom_PropertyGetBody()
        {
            MyClass myClass = new Test().MyClass;
            Assert.Equal(1, myClass.Field);
        }

        public MyClass MyClass
        {
            get
            {
                dynamic mc = new MemberClass();
                return (MyClass)mc.Method_ReturnMyClass();
            }
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass005.regclass005
        [Fact]
        public static void CalledFrom_CollectionInitializer()
        {
            dynamic mc = new MemberClass();
            object p2 = null;
            char p3 = 'a';
            decimal[] dec = new decimal[] { 1 };
            var list = new List<char?> { (char?)mc.Method_ReturnCharNullable(), (char?)mc.Method_ReturnCharNullable(dec, ref p2, ref p3) };

            Assert.Equal(3, ((MyClass)p2).Field);
            Assert.Equal('b', p3);
            Assert.Equal(new List<char?> { 'a', 'b' }, list);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass006.regclass006
        [Fact]
        public static void CalledFrom_CheckedExpression()
        {
            dynamic mc = new MemberClass();
            string s = "a";
            int m1 = checked((int)mc.Method_ReturnInt(null, null) + 1);
            int m2 = unchecked((int)mc.Method_ReturnInt(s) + int.MaxValue);
            Assert.Equal(2, m1);
            Assert.Equal(-2147483648, m2);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass007.regclass007
        [Fact]
        public static void CalledFrom_TryCatchFinally()
        {
            int? result1 = null;
            decimal result2 = 0;
            dynamic dy = new MemberClass();
            try
            {
                dy.Method_ReturnMyClass_Throw();
            }
            catch (ArithmeticException e)
            {
                result1 = (int?)dy.Method_ReturnIntNullable();
            }
            finally
            {
                result2 = (decimal)dy.Method_ReturnDecimal();
            }

            Assert.Equal(result1.Value, result2);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass008.regclass008
        [Fact]
        public static void CalledFrom_ExtensionArgument()
        {
            dynamic dy = new MemberClass();
            Assert.Equal(6, ((int)dy.Method_ReturnInt(null, new char[] { 'a' }, 0)).ExPlus());
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass009.regclass009
        [Fact]
        public static void CalledFrom_ThrowExpression()
        {
            bool passed = false;
            dynamic mc = new MemberClass();
            try
            {
                throw new ArithmeticException((string)mc.Method_ReturnString());
            }
            catch (ArithmeticException e)
            {
                passed = e.Message == "string";
            }
            Assert.True(passed);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass010.regclass010
        [Fact]
        public static void CalledFrom_UsingExpression()
        {
            dynamic mc = new MemberClass();
            string result = null;
            using (MemoryStream sm = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(sm))
                {
                    sw.WriteLine(mc.Method_ReturnString('a'));
                    sw.Flush();
                    sm.Position = 0;
                    using (StreamReader sr = new StreamReader(sm, (bool)mc.Method_ReturnBool()))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }

            Assert.Equal("a", result.ToString().Trim());
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass011.regclass011
        [Fact]
        public static void CalledFrom_LockExpression()
        {
            dynamic mc = new MemberClass();
            dynamic result = true;
            lock (s_locker = mc.Method_ReturnString(new char[] { 'a', 'b' }))
            {
                result = MemberClass.StaticMethod_ReturnBool();
            }
            Assert.False(result);
        }

        private static object s_locker;

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass012.regclass012
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55051", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void CalledFrom_ForExpressionBody()
        {
            dynamic mc = new MemberClass();
            sbyte result = 0;
            for (ulong i = (ulong)mc.Method_ReturnUlong(); i < (ulong)mc.Method_ReturnUlong(new MyClass()); i++)
            {
                result += (sbyte)mc.Method_ReturnSbyte((int)i, mc, (short)i, (decimal)i, new MyClass(), (float)i, (short?)i);
            }
            Assert.Equal(119, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass012a.regclass012a
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55051", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void CalledFrom_ForExpressionConditions()
        {
            dynamic mc = new MemberClass();
            sbyte result = 0;
            dynamic d = new MyClass();
            for (ulong i = mc.Method_ReturnUlong(); i < mc.Method_ReturnUlong(d); i++)
            {
                result += mc.Method_ReturnSbyte((int)i, mc, (short)i, (decimal)i, d, (float)i, (short?)i);
            }
            Assert.Equal(119, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass015.regclass015
        [Fact]
        public static void CalledFrom_DefaultSectionStatementList()
        {
            dynamic mc = new MemberClass();
            char c = '0';
            switch (c)
            {
                case '1':
                    break;
                default:
                    c = (char)mc.Method_ReturnChar(new MyStruct[] { new MyStruct() { Number = 0 }, new MyStruct() { Number = 1 } });
                    break;
            }
            Assert.Equal('b', c);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass018.regclass018
        [Fact]
        public static void CalledFrom_IteratorCallingLambdaExpression()
        {
            decimal index = 0;
            dynamic dy = new Test();
            foreach (decimal i in (IEnumerable)dy.Increment(0, 10))
            {
                Assert.Equal(i, index++);
            }
        }

        private static dynamic s_mc = new MemberClass();

        public IEnumerable Increment(int number, decimal max)
        {
            while (number < max)
            {
                Func<int?, decimal> func = (int? x) => (decimal)s_mc.Method_ReturnDecimal(x);
                yield return func(number++);
            }
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass019.regclass019
        [Fact]
        public static void CalledFrom_ObjectInitializerInsideCollectionInitializer()
        {
            dynamic mc = new MemberClass();
            List<Test> list = new List<Test>() { new Test() { _myclass = mc.Method_ReturnMyClass(11) } };
            Assert.Equal(1, list.Count);
            Assert.Equal(11, list[0]._myclass.Field);
        }

        private MyClass _myclass = null;

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass020.regclass020
        [Fact]
        public static void CalledFrom_TryCatchFinally_AnonymousMethod_ReferringToTwoDynamicParameters()
        {
            dynamic mc = new MemberClass();
            int result = -1;
            bool passed = false;
            try
            {
                Func<short, short, short, short> func = delegate (short x, short y, short z)
                {
                    return (short)mc.Method_ReturnShort_Throw((MyEnum)mc.Method_ReturnMyEnum(), mc.Method_ReturnMyEnum(x, (short)y), mc.Method_ReturnMyEnum((short)z));
                };
                result = func(1, 2, 3);
            }
            catch (ArithmeticException e)
            {
                passed = e.Message == "Test message";
            }
            finally
            {
                result = (short)mc.Method_ReturnUlong(new MyClass()) - 1;
            }
            Assert.True(passed);
            Assert.Equal(119, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass021.regclass021
        [Fact]
        public static void CalledFrom_AnonymousType_InsideQueryExpression()
        {
            List<int> list = new List<int>() { 0, 4, 1, 6, 4, 4, 5 };
            string s = "test";
            dynamic mc = new MemberClass();
            var result = list.Where(p => p == (int)mc.Method_ReturnInt(ref s)).Select(p => new { A = mc.Method_ReturnDecimalNullable(p) }).ToList();
            Assert.Equal("TEST", s);
            Assert.Equal(3, result.Count);
            foreach (var m in result)
            {
                Assert.Equal(4, (decimal)m.A);
                Assert.IsType<decimal>(m.A);
            }
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass021.regclass021
        [Fact]
        public static void CalledFrom_ForeachInsideUsingStatement()
        {
            int[] array = new int[] { 1, 2 };
            MemoryStream ms = new MemoryStream(new byte[] { 84, 101, 115, 116 });
            dynamic mc = new MemberClass();
            string result = string.Empty;
            byte? p1 = null;
            ulong? p2 = 0;
            int p3 = 10;
            using (dynamic sr = new StreamReader(ms, (bool)mc.Method_ReturnBool(out p1, ref p2, out p3)))
            {
                foreach (int s in array)
                {
                    ms.Position = 0;
                    string m = ((StreamReader)sr).ReadToEnd();
                    result += m + s.ToString();
                }
            }

            Assert.Equal(3, (byte)p1);
            Assert.Null(p2);
            Assert.Equal("Test1Test2", result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass023.regclass023
        [Fact]
        public static void CalledFrom_StaticMethodBody2()
        {
            dynamic mc = new MemberClass();
            decimal?[] p2 = null;
            var result = (bool?)mc.Method_ReturnBoolNullable(null, out p2, null, 0);
            Assert.Equal(2, p2.Length);
            Assert.True(result.Value);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass024.regclass024
        [Fact]
        public static void CalledFrom_StaticMethodBody3()
        {
            dynamic mc = new MemberClass();
            short? p = null;
            var result = (bool?)mc.Method_ReturnBoolNullable(p);
            Assert.Null(result);
            p = 0;
            Assert.True((bool?)mc.Method_ReturnBoolNullable(p));
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass025.regclass025
        [Fact]
        public static void CalledFrom_UsedInMethodArgument()
        {
            dynamic mc = new MemberClass();
            IsEqual((byte?[])mc.Method_ReturnByteArrNullable());
        }

        private static void IsEqual(byte?[] input)
        {
            Assert.Equal(10, input.Length);
            Assert.Equal(new byte?[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, input);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass026.regclass026
        [Fact]
        public static void CalledFrom_GenericMethodArgument()
        {
            dynamic mc = new MemberClass();
            IsEqual<Test>((byte?[])mc.Method_ReturnByteArrNullable(new MyStruct?[] { new MyStruct() { Number = 10 } }));
        }

        private static void IsEqual<T>(byte?[] input)
        {
            Assert.Equal(10, input.Length);
            Assert.Equal(new byte?[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, input);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass027.regclass027
        [Fact]
        public static void CalledFrom_StaticMethodBody4()
        {
            dynamic mc = new MemberClass();
            MyStruct? m = new MyStruct { Number = 10 };
            byte?[] result = (byte?[])mc.Method_ReturnByteArrNullable(out m);
            Assert.Equal(2, m.Value.Number);
            Assert.Equal(new byte?[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass028.regclass028
        [Fact]
        public static void CalledFrom_GenericStaticMethodBody1()
        {
            TestMethod<MyEnum>();
        }

        private static void TestMethod<T>()
        {
            dynamic mc = new MemberClass();
            string p1 = null;
            ulong? p2 = 20;
            byte?[] p3 = null;
            byte?[] result = (byte?[])mc.Method_ReturnByteArrNullable(out p1, ref p2, ref p3);
            Assert.Equal("one", p1);
            Assert.Null(p2);
            Assert.Equal(new byte?[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass029.regclass029
        [Fact]
        public static void CalledFrom_DelegateInvokeArgument()
        {
            dynamic mc = new MemberClass();
            MyDec dec = IsEqual;
            dec((byte?[])mc.Method_ReturnByteArrNullable(null, null, null));
        }

        private delegate void MyDec(byte?[] input);

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass030.regclass030
        [Fact]
        public static void CalledFrom_Constructor()
        {
            ClassWithConstructor1 t = new ClassWithConstructor1();
            Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, t._field);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass031.regclass031
        [Fact]
        public static void CalledFrom_FieldInitializerOutOfConstructor()
        {
            ClassWithConstructor2 t = new ClassWithConstructor2();
            Assert.Equal(new byte[] { 5, 5, 5 }, t._field);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass032.regclass032
        [Fact]
        public static void CalledFrom_StaticFieldInitializerOutOfConstructor()
        {
            Assert.Equal(new short?[] { 1, null }, ClassWithStaticConstructor.s_p1);
            Assert.Equal(new bool?[] { null }, ClassWithStaticConstructor.s_p2);
            Assert.Equal(MyEnum.Third, ClassWithStaticConstructor.s_p3);
            Assert.Equal(new byte[] { 1, 0 }, ClassWithStaticConstructor.s_field);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass034.regclass034
        [Fact]
        public static void CalledFrom_ImplicitlyTypeArrayInitalizer()
        {
            string p1 = "Test";
            dynamic mc = new MemberClass();
            var array = new char[] { (char)mc.Method_ReturnChar(ref p1, null, null), (char)mc.Method_ReturnChar(ref p1, 1, null) };
            Assert.Equal("test", p1);
            Assert.Equal(new char[] { 'c', 'c' }, array);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass035.regclass035
        [Fact]
        public static void CalledFrom_EqualityOperator()
        {
            dynamic mc = new MemberClass();
            MyClass myclass = new MyClass() { Field = 10 };
            bool b = ((char)mc.Method_ReturnCharNullable(myclass) == (char)mc.Method_ReturnCharNullable(out myclass));
            Assert.True(b);
            Assert.Equal(5, myclass.Field);
        }


        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass036.regclass036
        [Fact]
        public static void CalledFrom_PlusOperator()
        {
            ulong p1 = 0;
            MyEnum p2 = MyEnum.Second;
            dynamic mc = new MemberClass();
            Test t1 = new Test() { _field = (decimal)mc.Method_ReturnDecimal(out p1, ref p2, null, 'a') };
            Test t2 = new Test() { _field = (decimal)mc.Method_ReturnDecimal(null, null, 0) };
            Test t3 = t1 + t2;
            Assert.Equal((ulong)2, p1);
            Assert.Equal(MyEnum.Third, p2);
            Assert.Equal(1M, t1._field);
            Assert.Equal(1M, t2._field);
            Assert.Equal(2M, t3._field);
        }

        private decimal _field;

        public static Test operator +(Test t1, Test t2)
        {
            return new Test() { _field = t1._field + t2._field };
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass037.regclass037
        [Fact]
        public static void CalledFrom_StaticMethodBody5()
        {
            MyClass myclass0 = new MyClass() { Field = 0 };
            dynamic myclass1 = new MyClass() { Field = 1 };
            MyClass myclass2 = new MyClass() { Field = 2 };
            dynamic mc = new MemberClass();
            string s = ((object)mc.Method_ReturnCharNullable(new MyClass[] { myclass0, (MyClass)myclass1, myclass2 })).ToString();

            Assert.Equal("z", s);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass038.regclass038
        [Fact]
        public static void CalledFrom_ModuloOperator()
        {
            dynamic mc = new MemberClass();
            Test t1 = new Test() { _fieldModulo = (decimal?)mc.Method_ReturnDecimalNullable() };
            Test t2 = new Test() { _fieldModulo = (decimal?)mc.Method_ReturnDecimal(1, -1, 0) };
            Test t3 = t1 % t2;

            Assert.Equal(1M, t1._fieldModulo);
            Assert.Equal(1M, t2._fieldModulo);
            Assert.Equal(0M, t3._fieldModulo);
        }

        private decimal? _fieldModulo;

        public static Test operator %(Test t1, Test t2)
        {
            return new Test() { _fieldModulo = t1._fieldModulo % t2._fieldModulo };
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass039.regclass039
        [Fact]
        public static void CalledFrom_StaticMethodBody6()
        {
            int? p1 = 10;
            short? p2 = 20;
            MyStruct[] p3 = new MyStruct[1];
            dynamic mc = new MemberClass();
            decimal s = (decimal)mc.Method_ReturnDecimalNullable(ref p1, ref p2, out p3);
            Assert.Null(p1);
            Assert.Null(p2);
            Assert.Equal(2, p3.Length);
            Assert.Equal(3M, s);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass040.regclass040
        [Fact]
        public static void CalledFrom_ObjectInitializer_InsideCollectionInitializer()
        {
            int myInt = 345;
            dynamic mc = new MemberClass();
            List<Test> list = new List<Test>() { new Test() { _floatField = (float)mc.Method_ReturnFloat() }, new Test() { _floatField = (float)mc.Method_ReturnFloat(myInt) } };

            Assert.Equal(2, list.Count);
            Assert.Equal(3.4534f, list[0]._floatField);
            Assert.Equal(345f, list[1]._floatField);
        }

        private float _floatField;

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass041.regclass041
        [Fact]
        public static void CalledFrom_StaticPropertySetBody()
        {
            MyProp1 = 10f;
            Assert.Equal(10f, MyProp1);
        }

        private static float s_field;
        public static float MyProp1
        {
            get { return s_field; }
            set
            {
                float myFloat = 3f;
                dynamic mc = new MemberClass();
                s_field = (float)mc.Method_ReturnFloat(value, myFloat, 34.5f);
            }
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass042.regclass042
        [Fact]
        public static void CalledFrom_StaticGetPropertyBody()
        {
            float result = MyProp2;
            Assert.Equal(3, result);
            Assert.Equal(0.000003f, s_p1);
            Assert.Equal(2342424, s_p2);
        }

        private static float s_p1;
        private static float s_p2;
        public static float MyProp2
        {
            get
            {
                float myFloat = 3f;
                dynamic mc = new MemberClass();
                return (float)mc.Method_ReturnFloat(ref s_p1, out s_p2, s_p1, s_p2, myFloat);
            }
            set { }
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass043.regclass043
        [Fact]
        public static void CalledFrom_LockBlock()
        {
            dynamic mc = new MemberClass();
            int input = int.MinValue;
            float? result;
            lock (mc.Method_ReturnFloatNullable())
            {
                result = (float?)mc.Method_ReturnFloatNullable(input);
            }
            Assert.Equal(int.MinValue, result.Value);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass044.regclass044
        [Fact]
        public static void CalledFrom_ForLoopBody()
        {
            float[] array1 = new float[] { 1f, 24.333f, 3.44f };
            float?[] array2 = new float?[3];
            for (int i = 0; i < array1.Length; i++)
            {
                dynamic dy = new MemberClass();
                array2[i] = dy.Method_ReturnFloatNullable(array1[i], array1[0], array1[1], array1[2]);
            }

            Assert.Equal(array1[0], array2[0]);
            Assert.Equal(array1[1], array2[1]);
            Assert.Equal(array1[2], array2[2]);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass045.regclass045
        [Fact]
        public static void CalledFrom_DynamicVariableNamedDynamic()
        {
            float? p1 = null;
            float p2 = 1.234f;
            float? myFloat = null;
            dynamic dynamic = new MemberClass();
            float? result = (float?)dynamic.Method_ReturnFloatNullable(ref p1, out p2, p1, p2, myFloat);
            Assert.Equal(3f, result);
            Assert.Equal(0.000003f, p1);
            Assert.Equal(2342424, p2);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass046.regclass046
        [Fact]
        public static void CalledFrom_ForeachExpression()
        {
            dynamic mc = new MemberClass();
            List<float?> list = new List<float?>();
            foreach (var f in (float?[])mc.Method_ReturnFloatArrNullable())
            {
                list.Add(f);
            }
            Assert.Equal(new List<float?> { 3.4534f }, list);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass047.regclass047
        [Fact]
        public static void CalledFrom_GreaterThanOrEqualToOperator()
        {
            dynamic mc = new MemberClass();
            int intValue = 10;
            float floatValue = 9.9f;
            bool result = ((float?[])mc.Method_ReturnFloatArrNullable(intValue))[0] >= ((float?[])mc.Method_ReturnFloatArrNullable(floatValue, intValue))[0];
            Assert.True(result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass048.regclass048
        [Fact]
        public static void CalledFrom_StaticMethodBody7()
        {
            float p1 = 1f;
            float p2 = 2f;
            float myFloat = 0.111f;
            dynamic dynamic = new MemberClass();
            float?[] result = (float?[])dynamic.Method_ReturnFloatArrNullable(ref p1, out p2, p2, myFloat);
            Assert.Equal(new float?[] { 2 }, result);
            Assert.Equal(0.000003f, p1);
            Assert.Equal(2342424, p2);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass049.regclass049
        [Fact]
        public static void CalledFrom_GenericStaticMethodBody()
        {
            TestMethod<int, Test>();
        }

        private static void TestMethod<U, V>()
        {
            dynamic dy = new MemberClass();
            float[] result = (float[])dy.Method_ReturnFloatArr();
            Assert.Equal(new float[] { 3.4534f }, result);
        }

        // ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass05.regclass050
        [Fact]
        public static void CalledFrom_DoWhileExpression()
        {
            dynamic dy = new MemberClass();
            int value = 0;
            do { }
            while (((float[])dy.Method_ReturnFloatArr(value++))[0] < 10);
            Assert.Equal(11, value);
        }
    }

    public static class Extension
    {
        public static bool TestMethod(this Test t)
        {
            dynamic mc = new MemberClass();
            return mc.Method_ReturnBoolNullable();
        }

        public static int ExPlus(this object i)
        {
            int intValue = (int)i;
            return ++intValue;
        }
    }

    public class ClassWithConstructor1
    {
        public byte[] _field;
        public ClassWithConstructor1()
        {
            dynamic mc = new MemberClass();
            _field = (byte[])mc.Method_ReturnByteArr();
        }
    }

    public class ClassWithConstructor2
    {
        private dynamic _mc = new MemberClass();
        public byte[] _field;
        public ClassWithConstructor2()
        {
            _field = (byte[])_mc.Method_ReturnByteArr(new MyClass[] { null, null, new MyClass() });
        }
    }

    public class ClassWithStaticConstructor
    {
        private static dynamic s_mc = new MemberClass();
        public static short?[] s_p1 = null;
        public static bool?[] s_p2 = new bool?[1];
        public static MyEnum s_p3 = default(MyEnum);
        public static byte[] s_field = (byte[])s_mc.Method_ReturnByteArr(out s_p1, ref s_p2, ref s_p3);
        static ClassWithStaticConstructor() { }
    }
}

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass051.regclass051
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return new Test().TestMethod<Test>(float.MaxValue, float.NegativeInfinity, float.PositiveInfinity);
        }

        private int TestMethod<T>(float value1, float value2, float value3)
        {
            dynamic dy = new MemberClass();
            //This is also bad.... the same issue as 044...
            float[] result = (float[])dy.Method_ReturnFloatArr(value1, value2, value3);
            if (result.Length == 1 && result[0] == value1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass052.regclass052
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in regular method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            float p1 = 1f;
            float p2 = 2f;
            int result = new Test().TestMethod(ref p1, out p2, p1, p2);
            if (result == 0 && p1 == 0.000003f && p2 == 2342424)
                return 0;
            return 1;
        }

        private int TestMethod(ref float p1, out float p2, params float[] p3)
        {
            dynamic dy = new MemberClass();
            float[] result = (float[])dy.Method_ReturnFloatArr(ref p1, out p2, p3);
            if (result.Length == 1 && result[0] == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass053.regclass053
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in argument of extension method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            bool result = ((int?)dy.Method_ReturnIntNullable(true)).IsNull();
            if (result)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static bool IsNull(this int? value)
        {
            return value == null;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass054.regclass054
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in argument of regular method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            MyClass myclass = new MyClass();
            decimal p2 = -1M;
            char p3 = 'a';
            bool result = new Test().TestMethod((int?)dy.Method_ReturnIntNullable(out myclass, ref p2, p3));
            if (result)
                return 0;
            return 1;
        }

        public bool TestMethod(int? value)
        {
            if (value.Value == (int)'a')
                return true;
            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass055.regclass055
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in argument of static regular method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            bool value = true;
            bool result = Test.TestMethod((int?)dy.Method_ReturnIntNullable(true, false, value));
            if (result)
                return 0;
            return 1;
        }

        public static bool TestMethod(int? value)
        {
            if (value.Value == 1)
                return true;
            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass056.regclass056
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in argument of static generic method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            bool value = false;
            bool result = Test.TestMethod<bool>((int?)dy.Method_ReturnIntNullable(ref value));
            if (result && value)
                return 0;
            return 1;
        }

        public static bool TestMethod<T>(int? value)
        {
            if (value.Value == 1)
                return true;
            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass057.regclass057
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static unsafe method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int? s_p1 = null;
        private static short? s_p2 = 2;
        private static byte s_p3 = 0;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            bool result = Test.TestMethod((MyClass)dy.Method_ReturnMyClass(out s_p1, s_p2, out s_p3));
            if (result && s_p1 == 2 && s_p3 == 3)
                return 0;
            return 1;
        }

        private static bool TestMethod(MyClass mc)
        {
            return mc.Field == 2;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass058.regclass058
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in unsafe regular method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            ulong value = int.MaxValue;
            bool result = new Test().TestMethod((MyClass)dy.Method_ReturnMyClass(value), (int)value);
            if (result)
                return 0;
            return 1;
        }

        private bool TestMethod(MyClass mc, int value)
        {
            return mc.Field == value;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass059.regclass059
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            dynamic myS = new MyStruct()
            {
                Number = 10
            }

            ;
            MyClass result = (MyClass)dy.Method_ReturnMyClass(new MyStruct(), (MyStruct)myS);
            if (result.Field == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass060.regclass060
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in try expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                dynamic dy = new MemberClass();
                bool? value1 = true;
                dynamic dvalue1 = value1;
                MyClass result = (MyClass)dy.Method_ReturnMyClass_Throw((bool?)dvalue1);
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test exception"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass061.regclass061
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in try expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyClass p1 = new MyClass()
            {
                Field = 1
            }

            ;
            decimal?[] p2 = new decimal?[]
            {
            null
            }

            ;
            MyEnum p3 = MyEnum.Third;
            try
            {
                dynamic dy = new MemberClass();
                MyClass result = (MyClass)dy.Method_ReturnMyClass_Throw(out p1, out p2, p3);
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test exception") && p1.Field == 1 && p2.Length == 1 && p2[0] == null)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass062.regclass062
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in while body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool value = true;
            try
            {
                dynamic dy = new MemberClass();
                while (value)
                {
                    MyClass mc = dy.Method_ReturnMyClass_Throw(new byte?[0]);
                }
            }
            catch (ArithmeticException ae)
            {
                if (ae.Message == ("Test exception"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass063.regclass063
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool[] p1 = null;
            int[] p2 = new int[2];
            bool? p3 = true;
            dynamic dy = new MemberClass();
            MyEnum result = dy.Method_ReturnMyEnum(out p1, p2, out p3);
            if (p1.Length == 2 && p1[0] == true && p1[1] == false && p3 == null && result == MyEnum.First)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass064.regclass064
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in for loop-initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            int index = 0;
            int expected = 1;
            for (int i = (int)dy.Method_ReturnMyEnumNullable(); i <= (int)dy.Method_ReturnMyEnumNullable(new int[0]); i++)
            {
                if (i != expected)
                {
                    return 1;
                }
                expected++;

                index = i;
            }

            if (index == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass065.regclass065
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in while body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            int result = 0;
            int index = 0;
            do
            {
                result += (int)dy.Method_ReturnMyEnumNullable(new MyClass[0]);
            }
            while (index++ < 2);
            if (result == 9)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass065a.regclass065a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in while body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            dynamic result = 0;
            dynamic index = 0;
            do
            {
                result += ((MyEnum?)dy.Method_ReturnMyEnumNullable(new MyClass[0])).Value.GetHashCode();
            }
            while (index++ < 2);
            if (result == 9)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass066.regclass066
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool[] p1 = null;
            bool p2 = false;
            short[] p3 = null;
            dynamic dy = new MemberClass();
            MyEnum result = (MyEnum)dy.Method_ReturnMyEnumNullable(ref p1, p2, out p3);
            if (p1.Length == 2 && p3.Length == 3 && result == MyEnum.First)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass067.regclass067
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in foreach body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            string[] forLoop = new string[]
            {
            null, string.Empty, "Test"
            }

            ;
            List<MyStruct> list = new List<MyStruct>();
            foreach (string s in forLoop)
            {
                dynamic dy = new MemberClass();
                MyStruct result = (MyStruct)dy.Method_ReturnMyStruct(s, forLoop[0]);
                list.Add(result);
            }

            if (list.Count == 3 && list[0].Number == 3 && list[1].Number == 3 && list[2].Number == 3)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass068.regclass068
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            short p1 = 0;
            bool p2 = true;
            MyClass[] p3 = new MyClass[]
            {
            null, null
            }

            ;
            dynamic dy = new MemberClass();
            MyStruct result = (MyStruct)dy.Method_ReturnMyStruct(p1, out p2, p3);
            if (p2 == false && result.Number == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass069.regclass069
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in indexer-set.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private Dictionary<int, MyStruct> _dic = new Dictionary<int, MyStruct>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            t[string.Empty] = new MyStruct()
            {
                Number = 10
            }

            ;
            t["a"] = new MyStruct()
            {
                Number = -1
            }

            ;
            if (t._dic.Count == 2 && t._dic[0].Number == 0 && t._dic[1].Number == 1)
                return 0;
            return 1;
        }

        public MyStruct this[string i]
        {
            set
            {
                dynamic dy = new MemberClass();
                _dic.Add(i.Length, (MyStruct)dy.Method_ReturnMyStruct(i));
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass070.regclass070
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in dynamic method call.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            MyStruct? result = (MyStruct?)dy.Method_ReturnMyStructNullable((object)dy.Method_ReturnMyStructNullable());
            if (result.Value.Number == 3)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass071.regclass071
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            byte? p1 = null;
            string[] p2 = new string[2];
            dynamic dy = new MemberClass();
            MyStruct? result = (MyStruct?)dy.Method_ReturnMyStructNullable(out p1, out p2, ulong.MaxValue, ulong.MinValue);
            if (p1 == 3 && p2.Length == 3 && result.Value.Number == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass072.regclass072
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body and parameter contains dynamic.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            dynamic intdy = 1;
            dynamic enumdy = MyEnum.First;
            MyStruct? result = (MyStruct?)dy.Method_ReturnMyStructNullable((object)dy, (object)intdy, (object)enumdy);
            if (result.Value.Number == 3)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass073.regclass073
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in array initializer list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            MyEnum[] p1 = new MyEnum[]
            {
            MyEnum.First, default (MyEnum)}

            ;
            char?[] p3 = new char?[3];
            object[] result = new object[]
            {
            dy.Method_ReturnObject(), dy.Method_ReturnObject(new byte ? [0]), dy.Method_ReturnObject(p1, -1, ref p3), dy.Method_ReturnObject(new bool ? [4])}

            ;
            if (result.Length == 4 && result[0] == null && result[1] == null && result[2] == null && result[3] == null && p3[0] == 'a' && p3[1] == 'b')
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass074.regclass074
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in iterator and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;

    public class Test
    {
        private dynamic _mc = new MemberClass();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            try
            {
                foreach (short i in t.Increment(0))
                {
                    // never here.
                    return 1;
                }
            }
            catch (ArithmeticException ae)
            {
                if (ae.Message == "Test message")
                    return 0;
            }

            return 1;
        }

        public IEnumerable Increment(int number)
        {
            while (number < 5)
            {
                number++;
                yield return _mc.Method_ReturnShort_Throw();
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass076.regclass076
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in for and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var mc = new MemberClass();
            int index = 0;
            char? p1 = 'a';
            string[] p2 = new string[2];
            short[] p3 = new short[3];
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    mc.Method_ReturnShort_Throw(out p1, ref p2, p3); //exception
                    index++;
                }
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test message") && index == 0 && p1 == 'a' && p2.Length == 2)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass077.regclass077
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in constructor and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        public A()
        {
            dynamic mc = new MemberClass();
            Test.result = (short)mc.Method_ReturnShort_Throw(out Test.m);
        }
    }

    public class Test
    {
        public static MyEnum m = MyEnum.First;
        public static short result = -1;
        public Test()
        {
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                A t = new A(); //exception
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test message") && m == MyEnum.Second && result == -1)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass078.regclass078
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in using block and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.IO;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    dynamic dy = new MemberClass();
                    short? result = (short?)dy.Method_ReturnShort_ThrowNullable(); //exception
                }
            }
            catch (ArithmeticException ae)
            {
                if (ae.Message == "Test message")
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass079.regclass079
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in lock block and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private static object s_locker = new object();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                lock (s_locker)
                {
                    decimal? dec = 123M;
                    dynamic dy = new MemberClass();
                    short? result = (short?)dy.Method_ReturnShort_ThrowNullable(dec); //exception
                }
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test message"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass080.regclass080
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in do/while block and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private char? _p1 = null;
        private int[] _p2 = new int[2];
        private bool?[] _p3 = new bool?[6];


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                do
                {
                    Test t = new Test();
                    dynamic dy = new MemberClass();
                    short? result = (short?)dy.Method_ReturnShort_ThrowNullable(out t._p1, t._p2, t._p3); //exception
                }
                while (true);
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test message"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass081.regclass081
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in switch section and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                int a = 0;
                dynamic dy = new MemberClass();
                short? result = null;
                switch (a)
                {
                    case 0:
                        result = (short?)dy.Method_ReturnShort_ThrowNullable(null, 2M, null); //exception
                        break;
                    default:
                        break;
                }
            }
            catch (ArithmeticException ae)
            {
                if (ae.Message.Contains("Test message"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass082.regclass082
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in switch section and throw exception.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            decimal? d = 0M;
            try
            {
                string a = "a";
                dynamic dy = new MemberClass();
                short? result = null;
                switch (a)
                {
                    case "":
                        break;
                    default:
                        result = (short?)dy.Method_ReturnShort_ThrowNullable(ref d); //exception
                        break;
                }
            }
            catch (ArgumentException ae)
            {
                if (ae.Message.Contains("Test message") && d == 3M)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass083.regclass083
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            ulong?[] p1 = new ulong?[0];
            byte[] p2 = new byte[0];
            int?[] p3 = null;
            dynamic dy = new MemberClass();
            string result = (string)dy.Method_ReturnString(out p1, out p2, out p3);
            if (p1.Length == 2 && p2.Length == 2 && p3.Length == 3 && result == "c")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass084.regclass084
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in foreach.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            int index = 0;
            foreach (var s in (string[])dy.Method_ReturnStringArr())
            {
                index++;
            }

            if (index == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass085.regclass085
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static field init.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private static string[] s_result = (string[])s_dy.Method_ReturnStringArr(MyEnum.First);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_result.Length == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass086.regclass086
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in field init.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private string[] _result = (string[])s_dy.Method_ReturnStringArr(MyEnum.First, MyEnum.Second, MyEnum.Third);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result.Length == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass087.regclass087
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static string[] s_result;
        private static MyEnum? s_m = MyEnum.Third;
        static Test()
        {
            dynamic dy = new MemberClass();
            s_result = (string[])dy.Method_ReturnStringArr(ref s_m);
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_result.Length == 10 && Test.s_m == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass088.regclass088
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            short?[] p1 = null;
            MyStruct p2 = new MyStruct()
            {
                Number = 10
            }

            ;
            MyStruct[] p3 = new MyStruct[]
            {
            p2
            }

            ;
            dynamic mc = new MemberClass();
            string[] result = (string[])mc.Method_ReturnStringArr(p1, ref p2, out p3);
            if (p2.Number == 3 && p3.Length == 2 && p3[0].Number == 1 && p3[1].Number == 2 && result.Length == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass089.regclass089
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in object initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private long _field1;
        private long? _field2;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyClass p1 = new MyClass();
            dynamic mc = new MemberClass();
            Test t = new Test()
            {
                _field1 = (long)mc.Method_ReturnUlong(p1),
                _field2 = (long?)mc.Method_ReturnUlongNullable()
            }

            ;
            if (p1.Field == 0 && t._field1 == p1.ToString().Length && t._field2 == 1L)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass090.regclass090
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in implicitly-typed array initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            object[] p1 = null;
            byte[] p2 = null;
            decimal? p3 = null;
            var result = new long?[]
            {
            (long ? )mc.Method_ReturnUlongNullable(0), (long ? )mc.Method_ReturnUlongNullable(out p1, out p2, out p3), (long ? )mc.Method_ReturnUlongNullable('a', 'b', null)}

            ;
            if (result.Length == 3 && result[0] == 1L && result[1] == 1L && result[2] == 1L && p1.Length == 2 && p2.Length == 2 && p3 == 4M)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass091.regclass091
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular method used in == operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            MyClass[] m = new MyClass[]
            {
            null, null, new MyClass()}

            ;
            ulong?[] p1 = new ulong?[4];
            MyStruct p2 = new MyStruct();
            byte?[] p3 = new byte?[1];
            var result = (ulong)mc.Method_ReturnUlong(m) == (ulong)mc.Method_ReturnUlong(ref p1, out p2, ref p3);
            if (result && p1[0] == 3 && p2.Number == 3 && p3[0] == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass092.regclass092
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular void method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            mc.Method_ReturnVoid();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass093.regclass093
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular void method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            mc.Method_ReturnVoid(new MyStruct[]
            {
            new MyStruct(), new MyStruct()
            {
            Number = -1
            }
            }

            );
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass094.regclass094
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular void method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            mc.Method_ReturnVoid(1UL, 2UL);
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclass095.regclass095
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.regclass.regclassregmeth.regclassregmeth;
    // <Title> Tests regular class regular void method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            object[] p1 = new object[7];
            bool?[] p2 = new bool?[0];
            MyClass[] p3 = new MyClass[3];
            dynamic mc = new MemberClass();
            mc.Method_ReturnVoid(ref p1, out p2, ref p3);
            if (p1 == null && p2.Length == 1 && p2[0] == false && p3 == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}
