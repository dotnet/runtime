// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.extensionmethod004.extensionmethod004
{
    static // <Title>Extension method that extends dynamic</Title>
           // <Description>
           // </Description>
           // <RelatedBugs></RelatedBugs>
           //<Expects Status=success></Expects>
           // <Code>
public class MyClass
    {
        public static dynamic Foo(this int x)
        {
            return x;
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
            int x = 3;
            dynamic d = x.Foo();
            try
            {
                d.AnotherFoo(); //This will throw at runtime
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "AnotherFoo"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.extensionmethod005.extensionmethod005
{
    static // <Title>Extension method that extends dynamic</Title>
           // <Description>
           // </Description>
           // <RelatedBugs></RelatedBugs>
           //<Expects Status=success></Expects>
           // <Code>
public class MyClass
    {
        public static dynamic Foo(this int x, dynamic d)
        {
            return d;
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
            var x = 3;
            dynamic d = x.Foo(4);
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                d.Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.extensionmethod006.extensionmethod006
{
    static // <Title>Extension method that extends dynamic</Title>
           // <Description>
           // </Description>
           // <RelatedBugs></RelatedBugs>
           //<Expects Status=success></Expects>
           // <Code>
public class MyClass
    {
        public static string Foo(this object x, dynamic d)
        {
            return d.ToString();
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
            object x = "foo";
            dynamic d = x.Foo(4);
            if ((string)d != "4")
                return 1;
            try
            {
                d.Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "string", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.extensionmethod008.extensionmethod008
{
    // <Title>Extension method that extends dynamic</Title>
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
            Test t = new Test();
            dynamic d = 10;
            int result = t.Method(d);
            return result;
        }

        public int Method(int value)
        {
            return 0;
        }
    }

    public static class Extension
    {
        public static int Method(this Test t, int value)
        {
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method001.method001
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public void Foo(dynamic d)
            {
                string s = (string)d.ToString();
                if (s == "3")
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            mc.Foo(3);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method002.method002
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public void Foo(out object d)
            {
                d = 3;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic x;
            mc.Foo(out x);
            if ((int)x == 3)
                Test.s_ok = true;
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method003.method003
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public void Foo(dynamic d)
            {
                if ((string)d.ToString() == "3")
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            mc.Foo(3);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method004.method004
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public void Foo(ref dynamic d)
            {
                d = 4;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo(ref o);
            if ((string)o.ToString() == "4")
                Test.s_ok = true;
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method005.method005
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public void Foo(params dynamic[] d)
            {
                if (d.Length == 3)
                    Test.s_ok = true;
                if ((string)d[0].ToString() != "3")
                    Test.s_ok = false;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo(o, 1, null);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method007.method007
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public void Foo(dynamic d, dynamic d2)
            {
                if ((string)d.ToString() == "3" && (string)d2.ToString() == "5")
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            dynamic d = 5;
            mc.Foo(o, d);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method008.method008
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public void Foo(dynamic d, int d2)
            {
                if ((string)d.ToString() == "3" && d2 == 5)
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo(o, 5);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method009.method009
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public void Foo(dynamic d, dynamic d2, dynamic d3)
            {
                if ((string)d.ToString() == "3" && (string)d2.ToString() == "3" && (string)d3.ToString() == "3")
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo(o, o, o);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method010.method010
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public MyClass(params dynamic[] d)
            {
                if (d.Length == 3)
                    Test.s_ok = true;
                if ((string)d[0].ToString() != "3")
                    Test.s_ok = false;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic o = 3;
            MyClass mc = new MyClass(o, 1, null);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method011.method011
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public static void Foo(dynamic d)
            {
                string s = (string)d.ToString();
                if (s == "3")
                    Test.s_ok = true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass.Foo(3);
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method012.method012
{
    public class Test
    {
        private static bool s_ok = false;
        private class MyClass
        {
            public void Foo<T>(ref dynamic d)
            {
                d = default(T);
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo<int>(ref o);
            if ((string)o.ToString() == "0")
                Test.s_ok = true;
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method013.method013
{
    public class Test
    {
        private static bool s_ok = false;
        public class MyClass
        {
            public void Foo<T>(T d)
            {
                d = default(T);
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic o = 3;
            mc.Foo<dynamic>(o);
            if ((string)o.ToString() == "3")
                Test.s_ok = true;
            if (!Test.s_ok)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method015.method015
{
    // <Title> Having dynamic as a parameter</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var x = new C<int>();
            dynamic d = 3;
            x.M(d);
            if (Program.Status == 1)
                return 0;
            return 1;
        }
    }

    public class C<T>
    {
        public void M(T t)
        {
            Program.Status = 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method016.method016
{
    // <Title> Having more than 10 parameters but less than 32 should not crash the binder </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        public void Foo(int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10, int param11)
        {
            Program.Status = 1;
        }

        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Program();
            int x = 1;
            d.Foo(x, x, x, x, x, x, x, x, x, x, x);
            if (Program.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method017.method017
{
    // <Title> Having more than 10 parameters but less than 32 should not crash the binder </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        public void Foo(int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10, int param11, int param12, int param13, int param14, int param15, int param16, int param17, int param18, int param19, int param20, int param21, int param22, int param23, int param24, int param25, int param26, int param27, int param28, int param29, int param30, int param31)
        {
            Program.Status = 1;
        }

        public static int Status;

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Program();
            int x = 1;
            d.Foo(x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x);
            if (Program.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method018.method018
{
    // <Title> Having more than 10 parameters but less than 32 should not crash the binder </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        public void Foo(int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10, int param11, int param12, int param13, int param14, int param15, int param16, int param17, int param18, int param19, int param20, int param21, int param22, int param23, int param24, int param25, int param26, int param27, int param28, int param29, int param30, int param31, int param32)
        {
            Program.Status = 1;
        }

        public static int Status;

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Program();
            int x = 1;
            d.Foo(x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.method019.method019
{
    public class Test
    {
        private static bool s_ok = false;
        public void Bar()
        {
            Test.s_ok = false;
        }

        private static void Foo(dynamic d)
        {
            d.Bar();
            Test.s_ok = true;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo(new Test());
            if (Test.s_ok)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.genericbaseclass001.genericbaseclass001
{
    // <Title> Having dynamic as a type parameter in base class</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Base<T>
    {
        public int M()
        {
            return 100;
        }
    }

    public class Derived<V> : Base<dynamic>
    {
    }

    public class Base2<U, V>
    {
        virtual internal int M()
        {
            return 101;
        }
    }

    public class Derived2<U, V> : Base2<dynamic, V>
    {
        internal int P
        {
            get
            {
                return 111;
            }
        }

        internal override int M()
        {
            return base.M() + 1;
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
            var v1 = new Derived<string>();
            bool ret = (100 == v1.M());
            var v2 = new Derived2<bool, bool>();
            ret &= (111 == v2.P);
            var v3 = new Derived2<object, decimal>();
            ret &= (102 == v3.M());
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.genericbaseclass002.genericbaseclass002
{
    // <Title> Having dynamic as a type parameter in base class</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    namespace NS
    {
        public class MyClass1<T>
        {
        }

        public class MyClass2<U> : MyClass1<U>
        {
            public int this[int n]
            {
                get
                {
                    return ++n;
                }
            }
        }

        public class MyClass3<T> : MyClass2<dynamic>
        {
        }

        public class MyClass4<V> : MyClass3<V>
        {
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
                var v = new MyClass4<string>();
                bool ret = (100 == v[99]);
                var v2 = new MyClass3<dynamic>();
                ret &= (-99 == v2[-100]);
                return ret ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.genericbaseclass003.genericbaseclass003
{
    // <Title> Having dynamic as a type parameter in base class</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    namespace NS
    {
        public class MyClass1<T>
        {
        }

        public class MyClass11<U, V> : MyClass1<dynamic>
        {
            internal int M(int x, int y)
            {
                return x + y;
            }
        }

        public class MyClass12<U, V> : MyClass1<dynamic>
        {
            internal int M(int x, int y)
            {
                return x - y;
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
                var v1 = new MyClass11<object, object>();
                bool ret = (300 == v1.M(100, 200));
                var v2 = new MyClass12<dynamic, float>();
                ret &= (-100 == v2.M(100, 200));
                return ret ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.genericbaseclass004.genericbaseclass004
{
    // <Title> Having dynamic as a type parameter in base class</Title>
    // <Description>ICE</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    namespace NS
    {
        public interface I1
        {
        }

        public interface I2<T> : I1
        {
        }

        public interface I3<U, V>
        {
        }

        public class MyClass1<T> : I1, I2<T>
        {
            internal virtual int P
            {
                get
                {
                    return 123;
                }
            }
        }

        public class MyClass2<U, V> : MyClass1<dynamic>, I1, I2<V>
        {
        }

        public class MyClass3<X, Y, Z> : MyClass2<X, dynamic>, I3<X, Z>
        {
            internal new int P
            {
                get
                {
                    return base.P;
                }
            }
        }

        public class MyClass4<U, V> : MyClass1<dynamic>, I2<V>
        {
            internal override int P
            {
                get
                {
                    return 456;
                }
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
                var v1 = new MyClass3<byte, dynamic, char>();
                bool ret = (123 == v1.P);
                var v2 = new MyClass4<dynamic, dynamic>();
                ret = (456 == v2.P);
                return ret ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Methods.genericbaseclass005.genericbaseclass005
{
    // <Title> Having dynamic as a type parameter in base class</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    namespace NS
    {
        public interface I1
        {
        }

        public interface I2
        {
        }

        public interface I3<T> : I1, I2
        {
        }

        public interface I4<U, V> : I3<U>
        {
        }

        public class MyClass1<T> : I1, I2 // where T : new()
        {
            public MyClass1()
            {
            }
        }

        public class MyClass2<U, dynamic> : I1, I3<U> where U : class
        {
        }

        public class MyClass3<X, Y, Z> : MyClass2<X, dynamic>, I4<X, Z> where X : MyClass1<int> where Z : struct
        {
            internal int P
            {
                get
                {
                    return 123;
                }
            }
        }

        public class MyClass4<U, V> : MyClass1<dynamic>, I3<V> where U : new() where V : new()
        {
            internal int P
            {
                get
                {
                    return 456;
                }
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
                var v1 = new MyClass3<MyClass1<int>, byte, char>();
                bool ret = (123 == v1.P);
                var v2 = new MyClass4<int, uint>();
                ret = (456 == v2.P);
                return ret ? 0 : 1;
            }
        }
    }
    // </Code>
}
