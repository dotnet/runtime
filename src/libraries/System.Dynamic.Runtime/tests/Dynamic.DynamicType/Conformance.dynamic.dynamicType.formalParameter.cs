// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor001.cnstrctor001
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public MyClass(object o)
        {
            if ((int)o == 3)
                Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            MyClass mc = new MyClass(d);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor002.cnstrctor002
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public MyClass(dynamic o)
        {
            if (o != 3)
                Test.Status = 2;
            try
            {
                o.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    Test.Status = 2;
            }
        }
    }

    public class Test
    {
        public static int Status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object d = 3;
            MyClass mc = new MyClass(d);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor003.cnstrctor003
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public MyClass(int o)
        {
            if ((int)o == 3)
                Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            MyClass mc = new MyClass(d);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor004.cnstrctor004
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>choose right sig for new ctor(..dynamic..) - only bind for public method</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class MyType
    {
        public static int Result = 0;
        public MyType(byte p)
        {
            Result = 1;
        }

        public MyType(int p)
        {
            Result = 2;
        }

        public MyType(double p)
        {
            Result = 3;
        }

        public MyType(string p)
        {
            Result = 4;
        }

        public MyType(sbyte[] p1)
        {
            Result = 5;
        }

        public MyType(ushort p1, short p2, string p3)
        {
            Result = 6;
        }

        public MyType(ulong p1, long p2, object p3)
        {
            Result = 7;
        }

        public MyType(int[] p1, object p2, string[] p3)
        {
            Result = 8;
        }

        public MyType(uint[] p1, string p2, object[] p3)
        {
            Result = 9;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            bool ret = true;
            //
            dynamic d = (byte)255;
            var v = new MyType(d);
            ret = Varifier(1, "MyType(byte)");
            d = (int)-123456;
            v = new MyType(d);
            ret = Varifier(2, "MyType(int)");
            dynamic dd = (double)-0.123;
            v = new MyType(dd);
            ret = Varifier(3, "MyType(double)");
            d = "AAA";
            v = new MyType(d);
            ret = Varifier(4, "MyType(string)");
            d = new sbyte[]
            {
            1, 0, -1
            }

            ;
            v = new MyType(d);
            ret = Varifier(5, "MyType(sbyte[])");
            dynamic d1 = (ushort)4444;
            dynamic d2 = (short)-333;
            dynamic d3 = (string)String.Empty;
            v = new MyType(d1, d2, d3);
            ret = Varifier(6, "MyType(ushort, short, string)");
            d1 = (ulong)123456789;
            d2 = (long)-123456789;
            d3 = null;
            new MyType(d1, d2, d3);
            ret = Varifier(7, "MyType(ulong, long, object)");
            var d11 = new int[]
            {
            00, 11, 22, -12345
            }

            ;
            var d33 = new string[]
            {
            "A", "", String.Empty, null, "1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1"
            }

            ;

            new MyType(d11, 'c', d33);
            ret = Varifier(8, "MyType(int[], object, string[])");
            d1 = new uint[]
            {
            0, 1, 123456789
            }

            ;
            d2 = String.Empty;
            d3 = new object[]
            {
            0, null, -1.1, 'c', "123", 999999
            }

            ;
            v = new MyType(d1, d2, d3);
            ret = Varifier(9, "MyType(uint[], string, object[])");
            return ret ? 0 : 1;
        }

        public static bool Varifier(int expected, string output)
        {
            if (expected == MyType.Result)
                return true;
            System.Console.WriteLine("Not call " + output + ": ret={0}", MyType.Result);
            return false;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor007.cnstrctor007
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>choose right sig for new ctor(..dynamic..)</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface MyInterface
    {
    }

    public class MyClass : MyInterface
    {
    }

    public class MyDerivedClass : MyClass
    {
    }

    public struct MyStruct
    {
    }

    public enum MyEnum
    {
        one,
        two,
        three
    }

    public delegate void MyDelegate();
    public class MyType1
    {
        public static int Result = 0;
        public MyType1()
        {
            Result = 1;
        }

        public MyType1(int p1 = -1)
        {
            Result = 2;
        }

        public MyType1(int p1 = 0, int p2 = 1)
        {
            Result = 3;
        }

        public MyType1(int p1 = 0, int p2 = 1, int p3 = -1)
        {
            Result = 4;
        }

        public MyType1(params int?[] p)
        {
            Result = 5;
        }

        public MyType1(short p1, long p2 = -12345, string p3 = "AAA")
        {
            Result = 6;
        }

        public MyType1(params object[] p)
        {
            Result = 7;
        }

        public MyType1(string p = "@")
        {
            Result = 8;
        }
    }

    public class MyType2 : MyType1
    {
        public MyType2()
        {
            Result = 11;
        }

        public MyType2(MyDelegate p = null)
        {
            Result = 12;
        }

        public MyType2(char p1, MyInterface p2 = null)
        {
            Result = 13;
        }

        public MyType2(char p1, MyClass p2 = null)
        {
            Result = 14;
        }

        public MyType2(MyClass p1 = null, MyDerivedClass p2 = null, MyInterface p3 = null)
        {
            Result = 15;
        }

        public MyType2(MyEnum p1, MyEnum p2 = MyEnum.one)
        {
            Result = 16;
        }

        public MyType2(MyEnum[] p1, MyStruct[] p2 = null)
        {
            Result = 17;
        }

        public MyType2(float[] p1, float[] p2 = null, params float[] p3)
        {
            Result = 18;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            bool ret = true;
            int v1, v2, v3;
            v1 = (int)99;
            new MyType1(v1);
            ret &= Varifier(2, MyType1.Result, "MyType1(int=-1)");
            v1 = 10000;
            v2 = -100000;
            new MyType1(v1, v2);
            ret &= Varifier(3, MyType1.Result, "MyType1(int=0, int=1)");
            v1 = -11111;
            v2 = +22222;
            v3 = -33333;
            new MyType1(v1, v2, v3);
            ret &= Varifier(4, MyType1.Result, "MyType1(int=0, int=1, int=-1)");
            dynamic vn = new int?[]
            {
            1, 0, null, -1
            }

            ;
            new MyType1(vn);
            ret &= Varifier(5, MyType1.Result, "(1) MyType1(params int?[]");
            v1 = Int32.MaxValue;
            v2 = Int32.MinValue;
            v3 = unchecked(-int.MinValue + 1);
            dynamic v4 = -int.MaxValue + 1;
            new MyType1(v1, v2, v3, v4);
            ret &= Varifier(5, MyType1.Result, "(2) MyType1(params int[]");
            dynamic v11 = (short)short.MinValue;
            dynamic v12 = (long)long.MaxValue;
            dynamic v13 = "+0000000000000000000000000000000000000000000000000000000000+";
            new MyType1(v11, v12, v13); // call v11 [v12] -> 2 or 7
            ret &= Varifier(6, MyType1.Result, "(1) MyType1(short, long=-12345, string=\"AAA\")");
            new MyType1(v11, p2: Int64.MinValue); // assert but run
            ret &= Varifier(6, MyType1.Result, "(2) MyType1(short, long=-12345, string=\"AAA\")");
            new MyType1(v11, p3: "QQQQQQQQQQQQQQQQQQQQQQQ"); // assert but run
            ret &= Varifier(6, MyType1.Result, "(3) MyType1(short, long=-12345, string=\"AAA\")");
            dynamic v21 = new object[]
            {
            null, String.Empty, "", 'c', 0
            }

            ;
            new MyType1(v21);
            ret &= Varifier(7, MyType1.Result, "(1) MyType1(params object[])");
            dynamic v22 = (object)null;
            dynamic v23 = String.Empty;
            dynamic v24 = "";
            dynamic v25 = 'c';
            MyType1.Result = -1;
            //new MyType1(v22); 
            //ret &= Varifier(7, MyType1.Result, "(2) MyType1(params object[])");
            MyType1.Result = -1;
            new MyType1(v22, v23);
            ret &= Varifier(7, MyType1.Result, "(3) MyType1(params object[])");
            MyType1.Result = -1;
            new MyType1(v22, v23, v24);
            ret &= Varifier(7, MyType1.Result, "(4) MyType1(params object[])");
            MyType1.Result = -1;
            new MyType1(v22, v23, v24, v25);
            ret &= Varifier(7, MyType1.Result, "(5) MyType1(params object[])");
            new MyType1(v13);
            ret &= Varifier(8, MyType1.Result, "MyType1(string=\"@\")");
            ///////////////////////////////////////////////////////////////////////////////////
            dynamic vde = new MyDelegate(DelMethod);
            new MyType2(vde);
            ret &= Varifier(12, MyType2.Result, "MyType2(MyDelegate=null)");
            dynamic vi = (MyInterface)new MyDerivedClass();
            new MyType2('c', (MyInterface)vi);
            ret &= Varifier(13, MyType2.Result, "MyType2(char p1, MyInterface= null)");
            dynamic vc = new MyClass();
            char ch = 'q';
            new MyType2(ch, vc);
            ret &= Varifier(14, MyType2.Result, "MyType2(char p1, MyClass= null)");
            vc = new MyDerivedClass();
            dynamic vd = new MyDerivedClass();
            new MyType2(vc, vd, vi);
            ret &= Varifier(15, MyType2.Result, "MyType2(MyClass= null, MyDerivedClass= null, MyInterface= null)");
            dynamic ve = MyEnum.two;
            new MyType2(ve);
            ret &= Varifier(16, MyType2.Result, "MyType2(MyEnum p1, MyEnum=MyEnum.one)");
            //
            MyType2.Result = -1;
            new MyType2(ve, ve);
            ret &= Varifier(16, MyType2.Result, "MyType2(MyEnum p1, MyEnum=MyEnum.one)");
            dynamic ves = new MyEnum[]
            {
            }

            ;
            dynamic vs = new MyStruct[]
            {
            }

            ;
            new MyType2(ves);
            ret &= Varifier(17, MyType1.Result, "MyType2(MyEnum[], MyStruct[] = null)");
            //
            MyType2.Result = -1;
            new MyType2(ves, vs);
            ret &= Varifier(17, MyType1.Result, "MyType2(MyEnum[], MyStruct[] = null)");
            dynamic vf1 = new float[]
            {
            -1f
            }

            ;
            dynamic vf2 = new float[]
            {
            0f, 0.12345f, -0f, 1f, -1f
            }

            ;
            dynamic vf3 = new float[]
            {
            0f, 0.12345f, -12345.6789f, -0f, 1f, -1f
            }

            ;
            MyType2.Result = -1;
            new MyType2(vf1);
            ret &= Varifier(18, MyType2.Result, "MyType2(float[], float[]= null, params float[])");
            MyType2.Result = -1;
            new MyType2(vf1, vf2);
            ret &= Varifier(18, MyType2.Result, "MyType2(float[], float[]= null, params float[])");
            MyType2.Result = -1;
            new MyType2(vf1, vf2, vf3);
            ret &= Varifier(18, MyType2.Result, "MyType2(float[], float[]= null, params float[])");
            MyType2.Result = -1;
            new MyType2(vf2, vf3, 1.1f, 2.2f, 3.3f);
            ret &= Varifier(18, MyType2.Result, "MyType2(float[], float[]= null, params float[])");
            return ret ? 0 : 1;
        }

        public static bool Varifier(int expected, int actual, string output)
        {
            if (expected == actual)
                return true;
            System.Console.WriteLine("Not call " + output + ": ret={0}", actual);
            return false;
        }

        public static void DelMethod()
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor008.cnstrctor008
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>choose right sig for ctor declared with (..dynamic..)</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyType
    {
        public static int Result = 0;
        public MyType(dynamic p)
        {
            Result = 1;
        }

        internal MyType(object p1, dynamic p2)
        {
            Result = 2;
        }

        public MyType(ref dynamic p1, out dynamic p2)
        {
            p2 = default(dynamic);
            Result = 3;
        }

        internal MyType(int[] p1, dynamic[] p2)
        {
            Result = 4;
        }

        public MyType(ref dynamic[] p1, ref object[] p2)
        {
            Result = 5;
        }

        internal MyType(dynamic p1, dynamic p2, dynamic p3)
        {
            Result = 6;
        }

        public MyType(string p1, object p2, dynamic p3)
        {
            Result = 7;
        }

        public MyType(short p)
        {
            Result = 8;
        }
    }

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public struct Test
    {
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            bool ret = true;
            //
            var d = (short)255;
            var v = new MyType(d);
            ret = Varifier(8, "MyType(short)");
            dynamic n = -123456;
            v = new MyType(n);
            ret = Varifier(1, "MyType(dynamic->int)");
            MyType.Result = 0;
            var s = "AAA";
            v = new MyType(s);
            ret = Varifier(1, "MyType(dynamic->string)");
            MyType.Result = 0;
            float f = 0.123f;
            v = new MyType(f);
            ret = Varifier(1, "MyType(dynamic->float)");
            var v1 = new object();
            var v2 = new object();
            new MyType(v1, v2);
            ret = Varifier(2, "MyType(object, dynamic)");
            new MyType(ref v1, out v2);
            ret = Varifier(3, "MyType(ref object, out dynamic)");
            object v3 = 123;
            object v4 = 456;
            new MyType(v3, v4);
            ret = Varifier(2, "MyType(object, dynamic)");
            // CS1502/03
            new MyType(ref v3, out v4);
            ret = Varifier(3, "MyType(ref object, out dynamic)");
            var v5 = new int[]
            {
            1, 0, -1
            }

            ;
            var v6 = new object[]
            {
            null
            }

            ;
            v = new MyType(v5, v6);
            ret = Varifier(4, "MyType(int[], dynamic[])");
            object[] v7 = new object[]
            {
            0, null, 1, 'a'
            }

            ;
            v = new MyType(ref v7, ref v6);
            ret = Varifier(5, "MyType(ref dynamic[], ref object[])");
            MyType.Result = 0;
            v = new MyType(ref v6, ref v6);
            ret = Varifier(5, "MyType(ref dynamic[], ref object[])");
            v = new MyType(v1, v2, v3);
            ret = Varifier(6, "MyType(dynamic, dynamic, dynamic)");
            v = new MyType("QQQ", null, v3);
            ret = Varifier(7, "MyType(string, object, dynamic)");
            return ret ? 0 : 1;
        }

        public static bool Varifier(int expected, string output)
        {
            if (expected == MyType.Result)
                return true;
            System.Console.WriteLine("Not call " + output + ": ret={0}", MyType.Result);
            return false;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor009.cnstrctor009
{
    // <Title>Formal parameter to constructor</Title>
    // <Description>choose right sig for ctor declared with (..dynamic..)</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public interface MyInterface
    {
    }

    public class MyClass : MyInterface
    {
    }

    public class MyDerivedClass : MyClass
    {
    }

    internal struct MyStruct
    {
    }

    internal enum MyEnum
    {
        one,
        two,
        three
    }

    internal delegate void MyDelegate();
    public struct MyType
    {
        public static int Result = 0;
        public MyType(dynamic p)
        {
            Result = 1;
        }

        internal MyType(object p1, dynamic p2)
        {
            Result = 2;
        }

        public MyType(ref dynamic p1, out object p2)
        {
            p2 = default(object);
            Result = 3;
        }

        internal MyType(float?[] p1, dynamic[] p2)
        {
            Result = 4;
        }

        public MyType(ref dynamic[] p1, ref object[] p2)
        {
            Result = 5;
        }

        internal MyType(dynamic p1, dynamic p2, dynamic p3)
        {
            Result = 6;
        }

        public MyType(MyClass p1, MyDerivedClass p2, dynamic p3)
        {
            Result = 7;
        }

        internal MyType(MyEnum p1, params dynamic[] p2)
        {
            Result = 8;
        }
    }

    public class Test
    {
        public static void DelMethod()
        {
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            bool ret = true;
            //
            var d = (MyInterface)new MyClass();
            var v = new MyType(d);
            ret = Varifier(1, "MyType(dynamic->MyInterface)");
            dynamic n = new MyStruct();
            v = new MyType(n);
            ret = Varifier(1, "MyType(dynamic->MyStruct)");
            MyType.Result = 0;
            object o = null;
            v = new MyType(o);
            ret = Varifier(1, "MyType(dynamic->object)");
            MyType.Result = 0;
            v = new MyType(null);
            ret = Varifier(1, "MyType(dynamic->null)");
            var e = new MyEnum();
            new MyType(e);
            ret = Varifier(8, "MyType(MyEnum, params dynamic[])");
            o = MyEnum.three;
            new MyType(o);
            ret = Varifier(1, "MyType(dynamic->(object)MyEnum)");
            new MyType(MyEnum.three, null);
            ret = Varifier(8, "MyType(MyEnum, params dynamic[]->null)");
            MyType.Result = 0;
            new MyType(MyEnum.three, new MyClass(), new MyDerivedClass());
            ret = Varifier(8, "MyType(MyEnum, params dynamic[]->new class, new class)");
            object v1 = new MyClass();
            object v2 = new MyDerivedClass();
            new MyType(v1, v2);
            ret = Varifier(2, "MyType(object->MyClass, dynamic->MyDerivedClass)");
            new MyType(ref v1, out v2);
            ret = Varifier(3, "MyType(ref object->MyClass, out dynamic->MyDerivedClass)");
            v1 = new MyDelegate(DelMethod);
            v2 = new MyStruct();
            new MyType(v1, v2);
            ret = Varifier(2, "MyType(object->MyDelegate, dynamic->MyStruct)");
            // CS1502/03
            new MyType(ref v2, out v1);
            ret = Varifier(3, "MyType(ref object->MyStruct, out dynamic->MyDelegate)");
            float?[] v5 = new float?[]
            {
            1f, null, 0.0f, 1.23456f
            }

            ;
            dynamic[] v6 = new object[]
            {
            new MyDerivedClass(), null
            }

            ;
            v = new MyType(v5, v6);
            ret = Varifier(4, "MyType(float?[], dynamic[])");
            object[] v7 = new object[]
            {
            new MyStruct(), null, new MyClass()}

            ;
            object[] v8 = new object[]
            {
            new MyDerivedClass()}

            ;
            v = new MyType(ref v6, ref v7);
            ret = Varifier(5, "MyType(ref dynamic[]->obj[], ref object[])");
            MyType.Result = 0;
            v = new MyType(ref v7, ref v8);
            ret = Varifier(5, "MyType(ref dynamic[], ref object[])");
            v = new MyType(new MyStruct(), new MyDelegate(DelMethod), MyEnum.one);
            ret = Varifier(6, "MyType(dynamic, dynamic, dynamic)");
            v = new MyType(new MyClass(), new MyDerivedClass(), MyEnum.two);
            ret = Varifier(7, "MyType(MyClass, MyDerivedClass, dynamic)");
            return ret ? 0 : 1;
        }

        public static bool Varifier(int expected, string output)
        {
            if (expected == MyType.Result)
                return true;
            System.Console.WriteLine("Not call " + output + ": ret={0}", MyType.Result);
            return false;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor013.cnstrctor013
{
    // <Title>Casting a dynamic constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            D.DynamicCSharpRunTest();
        }
    }

    public class D
    {
        private static int s_rez = 0;
        public D()
        {
        }

        public D(int x)
        {
            D.s_rez++;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 3;
            D dd = (D)new D(d);
            return D.s_rez == 1 ? 0 : 1; // incremented once when we new D(0) to call the test method.
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor014.cnstrctor014
{
    // <Title>Formal parameter to private constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var t = Test.GetTest();
            return t.X == 4 ? 0 : 1;
        }
    }

    public class Test
    {
        public int X;
        private Test(int i)
        {
            this.X = i;
            System.Console.WriteLine("Test({0})", i);
        }

        public static Test GetTest()
        {
            return new Test((dynamic)4);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor015.cnstrctor015
{
    // <Title>Formal parameter to protected constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var t = Test.GetTest();
            return t.X == 4 ? 0 : 1;
        }
    }

    public class Test
    {
        public int X;
        protected Test(int i)
        {
            this.X = i;
            System.Console.WriteLine("Test({0})", i);
        }

        public static Test GetTest()
        {
            return new Test((dynamic)4);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor016.cnstrctor016
{
    // <Title>Formal parameter to protected constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var t = Test.GetTest();
            return t.X == 4 ? 0 : 1;
        }
    }

    public class Test
    {
        public int X;
        internal Test(int i)
        {
            this.X = i;
            System.Console.WriteLine("Test({0})", i);
        }

        public static Test GetTest()
        {
            return new Test((dynamic)4);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor017.cnstrctor017
{
    // <Title>Formal parameter to protected constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public class Foo
    {
        public Foo(string text)
        {
            System.Console.WriteLine(text);
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            test++;
            dynamic d = 123;
            try
            {
                new Foo(d);
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Foo.Foo(string)"))
                    success++;
            }

            test++;
            try
            {
                Foo(d);
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Program.Foo(string)"))
                    success++;
            }

            return test == success ? 0 : 1;
        }

        private static int Foo(string x)
        {
            return 4;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.cnstrctor018.cnstrctor018
{
    // <Title>Formal parameter to protected constructor</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dd = 4;
            try
            {
                var d = new D(dd);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "D.D(string)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }

    public abstract class C
    {
        public C(int x)
        {
        }

        public C()
        {
        }
    }

    public class D : C
    {
        public D(string s)
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.indexer001.indexer001
{
    // <Title>Formal parameter for indexer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public int GetValue(int x)
        {
            return x;
        }
    }

    public class MyClass
    {
        public object this[dynamic d]
        {
            set
            {
                Test.Status = (int)d.GetValue(1);
            }

            get
            {
                Test.Status = (int)d.GetValue(2);
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Foo();
            MyClass mc = new MyClass();
            object o = 1;
            mc[d] = o;
            if (Test.Status != 1)
                return 1;
            o = mc[d];
            if (Test.Status != 2 || (int)o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.indexer002.indexer002
{
    // <Title>Formal parameter for indexer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public int GetValue(int x)
        {
            return x;
        }
    }

    public class MyClass
    {
        public dynamic this[dynamic d]
        {
            set
            {
                Test.Status = (int)d.GetValue(value);
            }

            get
            {
                Test.Status = (int)d.GetValue(2);
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Foo();
            MyClass mc = new MyClass();
            object o = 1;
            mc[d] = o;
            if (Test.Status != 1)
                return 1;
            o = mc[d];
            if (Test.Status != 2 || (int)o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.indexer004.indexer004
{
    // <Title>Formal parameter for indexer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public int GetValue(int x)
        {
            return x;
        }
    }

    public class MyClass
    {
        public object this[object d]
        {
            set
            {
                if ((int)value == 1)
                    Test.Status = 1;
            }

            get
            {
                Test.Status = 2;
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Foo();
            MyClass mc = new MyClass();
            dynamic o = 1;
            mc[d] = o;
            if (Test.Status != 1)
                return 1;
            o = mc[d];
            if (Test.Status != 2 || (int)o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.indexer005.indexer005
{
    // <Title>Formal parameter for indexer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public int GetValue(int x)
        {
            return x;
        }
    }

    public class MyClass
    {
        public int this[dynamic d]
        {
            set
            {
                Test.Status = (int)d.GetValue(value);
            }

            get
            {
                Test.Status = (int)d.GetValue(2);
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object d = new Foo();
            MyClass mc = new MyClass();
            int o;
            mc[d] = 1;
            if (Test.Status != 1)
                return 1;
            o = mc[d];
            if (Test.Status != 2 || o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.indexer006.indexer006
{
    // <Title>Formal parameter for indexer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int this[object d]
        {
            set
            {
                Test.Status = 1;
            }

            get
            {
                Test.Status = 2;
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 2;
            MyClass mc = new MyClass();
            int o;
            mc[d] = 1;
            if (Test.Status != 1)
                return 1;
            o = mc[d];
            if (Test.Status != 2 || o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method001.method001
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(dynamic d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 3;
            mc.Foo(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method002.method002
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(object d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method003.method003
{
    // <Title>Formal parameter to static method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public static void Foo(dynamic d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            MyClass.Foo(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method004.method004
{
    // <Title>Formal parameter to static method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public static void Foo(object d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic o = 3;
            MyClass.Foo(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method005.method005
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo<T>(T d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo<object>(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method006.method006
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo<T>(T d)
        {
            MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 3;
            mc.Foo<dynamic>(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method007.method007
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
    {
        public void Foo(T d)
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass<dynamic> mc = new MyClass<dynamic>();
            object o = 3;
            mc.Foo(o);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method008.method008
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
    {
        public void Foo(T d)
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass<object> mc = new MyClass<object>();
            dynamic o = 3;
            mc.Foo(o);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method009.method009
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class CommonLibrary
    {
        public void Method1<T>(Dictionary<T, dynamic> d)
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status = 2;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            CommonLibrary c = new CommonLibrary();
            dynamic d = new Dictionary<dynamic, dynamic>()
            {
            }

            ;
            c.Method1(d);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.method010.method010
{
    // <Title>Formal parameter to generic method</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class CommonLibrary
    {
        public void Method1<T>(Dictionary<T, dynamic> d)
        {
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            CommonLibrary c = new CommonLibrary();
            dynamic d = 3;
            try
            {
                c.Method1(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate001.operate001
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Field;
        public static MyClass operator +(dynamic d, MyClass x)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 23;
            MyClass m = o + mc;
            if (m.Field != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate002.operate002
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Field;
        public static dynamic operator +(dynamic d, MyClass x)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 23;
            object m = o + mc;
            if (((MyClass)m).Field != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate003.operate003
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Field;
        public static object operator +(dynamic d, MyClass x)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 23;
            dynamic m = o + mc;
            if (((MyClass)m).Field != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate004.operate004
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Field;
        public static MyClass operator +(object d, MyClass x)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 23;
            MyClass m = o + mc;
            if (((MyClass)m).Field != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate006.operate006
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static dynamic operator !(MyClass p1)
        {
            object o = 3;
            return o;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = !mc;
            if (d != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.operate007.operate007
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static dynamic operator !(MyClass p1)
        {
            object o = 3;
            return o;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = !mc;
            if ((int)d != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.param001.param001
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(params dynamic[] d)
        {
            MyClass.Status = 1;
        }

        public void Foo(object o)
        {
            MyClass.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = 3;
            mc.Foo(d);
            // runtime type of d is 'object'
            if (MyClass.Status != 2)
                return 1;
            object o = 4;
            mc.Foo(o);
            if (MyClass.Status != 2)
                return 1;
            mc.Foo(o, d);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.param002.param002
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(dynamic d)
        {
            MyClass.Status = 1;
        }

        public void Foo(params object[] o)
        {
            MyClass.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = 3;
            mc.Foo(d);
            if (MyClass.Status != 1)
                return 1;
            object o = 4;
            mc.Foo(o);
            if (MyClass.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.param004.param004
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(ref dynamic d)
        {
            MyClass.Status = 1;
        }

        public void Foo(object o)
        {
            MyClass.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = 3;
            mc.Foo(ref d);
            if (MyClass.Status != 1)
                return 1;
            object o = 4;
            mc.Foo(o);
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.param006.param006
{
    // <Title>Formal parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public static int Status;
        public void Foo(out dynamic d)
        {
            MyClass.Status = 1;
            d = 3;
        }

        public void Foo(object o)
        {
            MyClass.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = 3;
            mc.Foo(out d);
            if (MyClass.Status != 1)
                return 1;
            object o = 4;
            mc.Foo(o);
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.param009.param009
{
    // <Title>Delegates</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new A();
            return x.Foo(1) ? 0 : 1;
        }

        public bool Foo(int x, params int[] y)
        {
            bool ret = x == 1;
            ret &= 0 == y.Length;
            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.property001.property001
{
    // <Title>Property that is of type dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public dynamic Foo
        {
            set
            {
                if (value != 3)
                    Test.Status = 2;
                try
                {
                    value.Foo();
                    Test.Status = 3;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                        Test.Status = 2;
                }
            }

            get
            {
                Test.Status = 2;
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object o = 3;
            mc.Foo = o;
            if (Test.Status != 1)
                return 1;
            Status = 1;
            o = mc.Foo;
            if (Test.Status != 2 || (int)o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.formalParameter.property002.property002
{
    // <Title>Property that is of type dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public object Foo
        {
            set
            {
                if ((int)value == 3)
                    Test.Status = 1;
            }

            get
            {
                Test.Status = 2;
                return 3;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo = o;
            if (Test.Status != 1)
                return 1;
            o = mc.Foo;
            if (Test.Status != 2 || (int)o != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}
