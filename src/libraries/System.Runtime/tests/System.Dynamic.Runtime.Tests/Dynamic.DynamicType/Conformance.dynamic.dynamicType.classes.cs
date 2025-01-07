// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.abstract001.abstract001
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public abstract class Base
    {
        public abstract int Foo(dynamic o);
    }

    public class Derived : Base
    {
        public override int Foo(object o)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(2);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.abstract002.abstract002
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public abstract class Base
    {
        public abstract int Foo(dynamic o);
    }

    public class Derived : Base
    {
        public override int Foo(object o)
        {
            Test.Status = 1;
            return 1;
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
            Base d = new Derived();
            d.Foo(2);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.abstract003.abstract003
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface IFoo
    {
        void Foo();
    }

    public abstract class Base
    {
        public abstract int Foo(dynamic o);
    }

    public class Derived : Base, IFoo
    {
        void IFoo.Foo()
        {
            Test.Status = 2;
        }

        public override int Foo(object o)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            IFoo x = d as IFoo;
            d.Foo(2);
            if (Test.Status != 1)
                return 1;
            x.Foo();
            if (Test.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.abstract004.abstract004
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public abstract class Base
    {
        public abstract int Foo(object o);
    }

    public class Derived : Base
    {
        public override int Foo(dynamic o)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(2);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.abstract005.abstract005
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public abstract class Base
    {
        public abstract int Foo(dynamic o);
    }

    public class Derived : Base
    {
        public override int Foo(dynamic o)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(2);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual001.virtual001
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int Foo(object o)
        {
            Test.Status = 2;
            return 1;
        }
    }

    public class Derived : Base
    {
        public override int Foo(dynamic d)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual002.virtual002
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int Foo(dynamic o)
        {
            Test.Status = 2;
            return 1;
        }
    }

    public class Derived : Base
    {
        public override int Foo(object d)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual003.virtual003
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int Foo(dynamic o)
        {
            Test.Status = 2;
            return 1;
        }
    }

    public class Derived : Base
    {
        public override int Foo(dynamic d)
        {
            Test.Status = 1;
            return 1;
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
            Derived d = new Derived();
            d.Foo(3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual004.virtual004
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int Foo(object o)
        {
            Test.Status = 3;
            return 1;
        }
    }

    public class Derived : Base
    {
        public override int Foo(dynamic d)
        {
            Test.Status = 2;
            return 1;
        }
    }

    public class FurtherDerived : Derived
    {
        public override int Foo(object d)
        {
            Test.Status = 1;
            return 1;
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
            FurtherDerived d = new FurtherDerived();
            d.Foo(3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual005.virtual005
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int Foo(dynamic o)
        {
            Test.Status = 3;
            return 1;
        }
    }

    public class Derived : Base
    {
        public override int Foo(object d)
        {
            Test.Status = 2;
            return 1;
        }
    }

    public class FurtherDerived : Derived
    {
        public override int Foo(dynamic d)
        {
            Test.Status = 1;
            return 1;
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
            FurtherDerived d = new FurtherDerived();
            d.Foo(3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.virtual006.virtual006
{
    // <Title>Classes</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public virtual int this[object o]
        {
            get
            {
                Test.Status = 2;
                return 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[dynamic d]
        {
            get
            {
                Test.Status = 1;
                return 1;
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
            Derived d = new Derived();
            int x = d[3];
            if (Test.Status != 1 || x != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.simplename001.simplename001
{
    // <Title>Classes - Simple Name (Method) Calling</Title>
    // <Description> ICE </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    namespace NS
    {
        public class C
        {
            public static int Bar(C t)
            {
                return 1;
            }

            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                dynamic v2 = new C();
                return (1 == Bar(v2)) ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.simplename003.simplename003
{
    // <Title>Classes - Simple Name (Method) Calling</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    namespace NS
    {
        public abstract class Base
        {
            protected static int result = 0;
            public static bool Bar(object o)
            {
                result = 1;
                return false;
            }

            public abstract int Foo(dynamic o);
            public virtual int Hoo(object o)
            {
                return 0;
            }

            public virtual int Poo(int n, dynamic o)
            {
                return 0;
            }
        }

        public class Derived : Base
        {
            public override int Foo(object o)
            {
                result = 2;
                return 0;
            }

            public new int Hoo(dynamic o)
            {
                result = 3;
                return 0;
            }

            public sealed override int Poo(int n, dynamic o)
            {
                result = 4;
                return 0;
            }

            public bool RunTest()
            {
                bool ret = true;
                dynamic d = new Derived();
                Bar(d);
                ret &= (1 == result);
                Foo(d);
                ret &= (2 == result);
                Hoo(d);
                ret &= (3 == result);
                dynamic x = 100;
                Poo(x, d);
                ret &= (4 == result);
                return ret;
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
                var v = new Derived();
                return v.RunTest() ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.simplename004.simplename004
{
    // <Title>Classes - Simple Name (Method) Calling</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    namespace NS
    {
        public struct MyType
        {
            public static int Bar<T>(object o)
            {
                return 1;
            }

            public int Foo<U>(object p1, object p2)
            {
                return 2;
            }

            public int Har<V>(object o, params int[] ary)
            {
                return 3;
            }

            public int Poo<W>(object p1, ref object p2, out dynamic p3)
            {
                p3 = default(dynamic);
                return 4;
            }

            public int Dar<Y>(object p1, int p2 = 100, long p3 = 200)
            {
                return 5;
            }

            public bool RunTest()
            {
                bool ret = true;
                dynamic d = null;
                ret &= (1 == Bar<string>(d));
                ret &= (2 == Foo<double>(d, d));
                d = new MyType();
                dynamic d1 = 999;
                ret &= (1 == Bar<float>(d));
                ret &= (2 == Foo<ulong>(d, d1));
                ret &= (3 == Har<long>(d));
                ret &= (4 == Poo<short>(d, ref d1, out d1));
                ret &= (3 == Har<long>(d, d1));
                ret &= (5 == Dar<char>(d));
                ret &= (5 == Dar<char>(d, p2: 100));
                ret &= (5 == Dar<char>(d, p3: 99, p2: 88));
                return ret;
            }
        }

        public class Test
        {

            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod(null));
            }

            public static int MainMethod(string[] args)
            {
                var v = new MyType();
                return v.RunTest() ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.simplename005.simplename005
{
    // <Title>Classes - Simple Name (Method) Calling</Title>
    // <Description> ICE </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    namespace NS1
    {
        public delegate void MyDel01(object p1);
        public delegate void MyDel02(string p1);
        public delegate bool MyDel03(object p1, string p2);
        public delegate bool MyDel04(string p1, string p2);
        public delegate void MyDel05(string p1, out int p2, ref long p3);
        public delegate void MyDel06(object p1, out int p2, ref long p3);
        public delegate int MyDel07(int p1, int p2 = -1, int p3 = -2);
        public delegate int MyDel08(object p1, byte p2 = 101);
        public delegate int MyDel09(object p1, int p2 = 1, long p3 = 2);
        public class Test
        {
            private static int s_result = -1;
            public void MyMtd01(object p1)
            {
                s_result = 1;
            }

            public void MyMtd02(string p1)
            {
                s_result = 2;
            }

            public bool MyMtd03(object p1, string p2)
            {
                s_result = 3;
                return true;
            }

            public bool MyMtd04(string p1, string p2)
            {
                s_result = 4;
                return false;
            }

            public void MyMtd05(string p1, out int p2, ref long p3)
            {
                s_result = 5;
                p2 = -30;
            }

            public void MyMtd06(object p1, out int p2, ref long p3)
            {
                s_result = 6;
                p2 = -40;
            }

            public int MyMtd07(int p1, int p2 = -10, int p3 = -20)
            {
                s_result = 7;
                return p3;
            }

            public int MyMtd08(object p1, byte p2 = 99)
            {
                s_result = 8;
                return 1;
            }

            public int MyMtd09(object p1, int p2 = 10, long p3 = 20)
            {
                s_result = 9;
                return p2;
            }

            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                var t = new Test();
                return (t.RunTest()) ? 0 : 1;
            }

            public bool RunTest()
            {
                dynamic v1 = new MyDel01(MyMtd01);
                dynamic d = null;
                dynamic d1 = "12345";
                v1(d);
                bool ret = (1 == s_result);
                v1 = new MyDel02(MyMtd02);
                v1(d);
                ret &= (2 == s_result);
                v1 = new MyDel03(MyMtd03);
                v1(d, d1);
                ret &= (3 == s_result);
                v1 = new MyDel04(MyMtd04);
                v1(d, d1);
                ret &= (4 == s_result);
                d = 12345;
                v1 = new MyDel05(MyMtd05);
                //v1(d1, out d, ref d); // by design
                //ret &= (5 == result);
                v1 = new MyDel06(MyMtd06);
                //v1(d1, out d, ref d);
                //ret &= (6 == result);
                v1 = new MyDel07(MyMtd07);
                v1(d);
                ret &= (7 == s_result);
                s_result = -1;
                v1(d, p3: 100);
                ret &= (7 == s_result);
                s_result = -1;
                v1(d, p3: 100, p2: 200);
                ret &= (7 == s_result);
                d = null;
                v1 = new MyDel08(MyMtd08);
                v1(d); //
                ret &= (8 == s_result);
                v1 = new MyDel09(MyMtd09);
                v1(d); //
                ret &= (9 == s_result);
                return ret;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.classes.memberaccess002.memberaccess002
{
    // <Title> Operator -.is, as</Title>
    // <Description>(By Design)</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = DayOfWeek.Monday;
            try
            {
                var y = x.value__;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) // Should not EX
            {
                System.Console.WriteLine(e);
                return 1;
            }

            return 0;
        }
    }
    //</Code>
}
