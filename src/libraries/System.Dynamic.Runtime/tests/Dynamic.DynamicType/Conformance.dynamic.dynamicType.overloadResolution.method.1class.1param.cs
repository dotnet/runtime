// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.param012.param012
{
    // <Title>Call methods that have different parameter modifiers with dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(23,23\).*CS0649</Expects>
    public struct myStruct
    {
        public int Field;
    }

    public class Foo
    {
        public void Method(params int[] x)
        {
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
            Foo f = new Foo();
            dynamic d = "foo";
            try
            {
                f.Method(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Foo.Method(params int[])"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.param014.param014
{
    // <Title>Call methods that have different parameter modifiers with dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct myStruct
    {
        public int Field;
    }

    public class Foo
    {
        public void Method(params int[] x)
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
            Foo f = new Foo();
            dynamic d = "foo";
            dynamic d2 = 3;
            try
            {
                f.Method(d2, d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Foo.Method(params int[])"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.param022.param022
{
    // <Title>Make sure that the cache is not blown away by the rules the binder generates</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success><Expects>
    // <Code>
    using System;

    public class A
    {
        public Action Baz
        {
            get;
            set;
        }
    }

    public class C
    {
        public void Baz()
        {
            Program.Status = 1;
        }
    }

    public class D<T>
    {
        public T Baz;
    }

    public struct E
    {
        public dynamic Baz;
    }

    public class F : I
    {
        public void Baz()
        {
            System.Console.WriteLine("E.Baz");
            Program.Status = 1;
        }

        public Action Bar
        {
            get;
            set;
        }

        public dynamic Foo
        {
            get;
            set;
        }
    }

    public interface I
    {
        void Baz();
        Action Bar
        {
            get;
            set;
        }

        dynamic Foo
        {
            get;
            set;
        }
    }

    public class Program
    {
        public static int Status = 0;
        private static void CallBaz(dynamic x)
        {
            x.Baz();
        }

        private static void CallBar(dynamic x)
        {
            x.Bar();
        }

        private static void CallFoo(dynamic x)
        {
            x.Foo();
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var a = new C();
            var b = new
            {
                Baz = new Action(() => Program.Status = 1)
            }

            ;
            var d = new D<Func<int>>()
            {
                Baz = new Func<int>(() => Program.Status = 1)
            }

            ;
            var e = new E()
            {
                Baz = new Action(() => Program.Status = 1)
            }

            ;
            var x = new
            {
                Baz = (Action)delegate
                {
                    Program.Status = 1;
                }
            }

            ;
            var f = new F();
            int rez = 0;
            int tests = 0;
            tests++;
            Program.Status = 0;
            x.Baz();
            rez += Program.Status;
            f.Bar = f.Baz;
            f.Foo = (Action)f.Baz;
            I i = f;
            tests++;
            Program.Status = 0;
            CallBar(i);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallFoo(i);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(b);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(b);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(d);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(b);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(e);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(d);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(b);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(i);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(i);
            rez += Program.Status;
            tests++;
            Program.Status = 0;
            CallBaz(a);
            rez += Program.Status;
            return rez == tests ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.nullable001.nullable001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            int? x = 5;
            int rez = d.M(x) == 1 ? 0 : 1; //we pass in the compile time type
            x = null;
            rez += d.M(x) == 1 ? 0 : 1; //we pass in the compile time type
            x = 5;
            dynamic dyn = x;
            rez += d.M(dyn) == 2 ? 0 : 1; //we boxed the int -> runtime type is int
            x = null;
            dyn = x;
            rez += d.M(dyn) == 1 ? 0 : 1; //we are passing in null -> the only overload is the nullable one
            return rez > 0 ? 1 : 0;
        }

        public int M(int? x)
        {
            return 1;
        }

        public int M(int x)
        {
            return 2;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.regr001.regr001
{
    // <Title>Overload resolution of methods involving pointer types</Title>
    // <Description>
    // Method overload resolution with dynamic argument resolving to array parameter with the corresponding pointer type as the parameter for the other method
    // </Description>
    //<Expects Status=warning>\(19,9\).*CS0169</Expects>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //using System;
    //enum color { Red, Blue, Green };
    //public struct S
    //{
    //int i;
    //}
    //class Program
    //{
    //unsafe public static int Test1(char* c)
    //{
    //return 1;
    //}
    //public static int Test1(char[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test2(int* c)
    //{
    //return 1;
    //}
    //public static int Test2(int[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test3(byte* c)
    //{
    //return 1;
    //}
    //public static int Test3(byte[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test4(sbyte* c)
    //{
    //return 1;
    //}
    //public static int Test4(sbyte[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test5(short* c)
    //{
    //return 1;
    //}
    //public static int Test5(short[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test6(ushort* c)
    //{
    //return 1;
    //}
    //public static int Test6(ushort[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test7(uint* c)
    //{
    //return 1;
    //}
    //public static int Test7(uint[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test8(long* c)
    //{
    //return 1;
    //}
    //public static int Test8(long[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test9(ulong* c)
    //{
    //return 1;
    //}
    //public static int Test9(ulong[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test10(float* c)
    //{
    //return 1;
    //}
    //public static int Test10(float[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test11(double* c)
    //{
    //return 1;
    //}
    //public static int Test11(double[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test12(decimal* c)
    //{
    //return 1;
    //}
    //public static int Test12(decimal[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test13(bool* c)
    //{
    //return 1;
    //}
    //public static int Test13(bool[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test14(color* c)
    //{
    //return 1;
    //}
    //public static int Test14(color[] c)
    //{
    //return 0;
    //}
    //unsafe public static int Test15(S* c)
    //{
    //return 1;
    //}
    //public static int Test15(S[] c)
    //{
    //return 0;
    //}
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod());} public static int MainMethod()
    //{
    //dynamic c1 = "Testing".ToCharArray();
    //if (Test1(c1) == 1) return 1;
    //dynamic c2 = new int[] { 4, 5, 6 };
    //if (Test2(c2) == 1) return 1;
    //dynamic c3 = new byte[] { 4, 5, 6 };
    //if (Test3(c3) == 1) return 1;
    //dynamic c4 = new sbyte[] { 4, 5, 6 };
    //if (Test4(c4) == 1) return 1;
    //dynamic c5 = new short[] { 4, 5, 6 };
    //if (Test5(c5) == 1) return 1;
    //dynamic c6 = new ushort[] { 4, 5, 6 };
    //if (Test6(c6) == 1) return 1;
    //dynamic c7 = new uint[] { 4, 5, 6 };
    //if (Test7(c7) == 1) return 1;
    //dynamic c8 = new long[] { 4, 5, 6 };
    //if (Test8(c8) == 1) return 1;
    //dynamic c9 = new ulong[] { 4, 5, 6 };
    //if (Test9(c9) == 1) return 1;
    //dynamic c10 = new float[] { 4.5f, 5.6f, 6.7f };
    //if (Test10(c10) == 1) return 1;
    //dynamic c11 = new double[] { 4.5d, 5.4d, 6.9d };
    //if (Test11(c11) == 1) return 1;
    //dynamic c12 = new decimal[] { 4.5m, 5.3m, 6.8m };
    //if (Test12(c12) == 1) return 1;
    //dynamic c13 = new bool[] { true };
    //if (Test13(c13) == 1) return 1;
    //dynamic c14 = new color[] {  };
    //if (Test14(c14) == 1) return 1;
    //dynamic c15 = new S[] { };
    //if (Test15(c15) == 1) return 1;
    //return 0;
    //}
    //}
    // </Code>
}
#if CAP_PointerType

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.regr002.regr002
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.method.Oneclass.Oneparam.regr002.regr002;
    // <Title>Overload resolution of methods involving pointer types</Title>
    // <Description>
    // Method overload resolution with dynamic argument resolving to array parameter with the corresponding pointer type as the parameter for the other method
    // </Description>

    // <RelatedBugs></RelatedBugs>

    //<Expects Status=success></Expects>

    // <Code>
    using System;

    public class Program
    {
        [Test]
        [Priority(Priority.Priority2)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
        public static int MainMethod()
        {
            var c = "abc".ToCharArray();
            var a = new string(c);
            var b = new string((dynamic)c);

            return 0;
        }
    }

    // </Code>
}
 #endif
