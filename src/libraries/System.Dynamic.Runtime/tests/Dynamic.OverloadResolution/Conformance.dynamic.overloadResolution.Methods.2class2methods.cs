// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.hide003.hide003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public void Method(C x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public new void Method(C x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            C x = new C();
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload001.overload001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public void Method(short x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            short x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload002.overload002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public void Method(short x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload003.overload003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        protected void Method(short x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload004.overload004
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        protected void Method(short x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            short x = 3;
            d.Method(x);
            if (Base.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload005.overload005
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        private void Method(short x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            short x = 3;
            d.Method(x);
            if (Base.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload006.overload006
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        internal void Method(short x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload007.overload007
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        protected internal void Method(short x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload008.overload008
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public int Method(dynamic x)
        {
            Base.Status = 1;
            return 1;
        }

        public void Method(short x)
        {
            Base.Status = 2;
        }
    }

    public class Derived : Base
    {
        public void Method(int x)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            dynamic x = 3;
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload009.overload009
{
    // <Title> Tests overload resolution</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class B
    {
        public static int status = -1;
        public void Foo(out int x)
        {
            status = 1;
            x = 1;
        }
    }

    public class A : B
    {
        public void Foo(ref int x)
        {
            status = 2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            int x;
            // used to call 'ref', should call 'out'
            a.Foo(out x);
            return (1 == status) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload010.overload010
{
    // <Title> Tests overload resolution</Title>
    // <Description>regression test </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class B
    {
        public static int status = -1;
        public void Foo(out int x)
        {
            status = 1;
            x = 1;
        }
    }

    public class A : B
    {
        public void Foo(ref int x)
        {
            status = 2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            int x;
            // used to call 'ref', should call 'out'
            a.Foo(out x);
            return (1 == status) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload011.overload011
{
    //<Area></Area>
    //<Title></Title>
    //<Description>regression test</Description>
    //<Related Bugs></Related Bugs>
    //<Expects Status=success></Expects Status>
    //<Code>
    public class NO
    {
        public void Foo<T, U>(U u = default(U), T t2 = default(T))
        {
            System.Console.WriteLine(1);
        }

        public void Foo<T>(params T[] t)
        {
            Program.result = 0;
        }
    }

    public class Program
    {
        public static int result;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic x = new NO();
            x.Foo(3);
            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload012.overload012
{
    //<Area></Area>
    //<Title></Title>
    //<Description>regression test</Description>
    //<Related Bugs></Related Bugs>
    //<Expects Status=success></Expects Status>
    //<Code>
    public class NO
    {
        public void Foo<T>(T t = default(T))
        {
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
            dynamic d = new NO();
            try
            {
                d.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return 0;
            }
            catch (System.Exception)
            {
                return 1;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.overload013.overload013
{
    // <Area>dynamic</Area>
    // <Title>ref parameter overloading</Title>
    // <Description>
    //   binder generates bad expression tree in presence of ref overload
    // </Description>
    // <Related Bugs></Related Bugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public static int x = 0;
        public void Foo(ref string y)
        {
            x = 1;
        }
    }

    public class C : A
    {
        public void Foo(string y)
        {
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new C();
            string y = "";
            x.Foo(ref y);
            return (A.x == 1) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr001.ovr001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(int x)
        {
            Base.Status = 2;
        }

        public void Method(long l)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr002.ovr002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(int x)
        {
            Base.Status = 2;
        }

        public void Method(long l)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            short x = 3;
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr003.ovr003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual void Method(string x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(string x)
        {
            Base.Status = 2;
        }

        public void Method(object o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            string x = "3";
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr004.ovr004
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(D x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(D x)
        {
            Base.Status = 2;
        }

        public void Method(C o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            C x = new C();
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr005.ovr005
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(D x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(D x)
        {
            Base.Status = 2;
        }

        public void Method(C o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            D x = new D();
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr006.ovr006
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(D x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(D x)
        {
            Base.Status = 2;
        }

        public void Method(C o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            C x = new D();
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr007.ovr007
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(int x)
        {
            Base.Status = 2;
        }

        public void Method(string o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr008.ovr008
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(int x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(int x)
        {
            Base.Status = 2;
        }

        public void Method(C o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            short x = 3;
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr009.ovr009
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public enum E
    {
        first,
        second
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(string x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(string x)
        {
            Base.Status = 2;
        }

        public void Method(E o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            string x = "adfs";
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr010.ovr010
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public static implicit operator int (C c)
        {
            return 0;
        }
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(C x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(C x)
        {
            Base.Status = 2;
        }

        public void Method(int o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            C x = new C();
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr011.ovr011
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public static implicit operator int (C c)
        {
            return 0;
        }
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(C x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(C x)
        {
            Base.Status = 2;
        }

        public void Method(long o)
        {
            Base.Status = 3;
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
            dynamic d = new Derived();
            C x = new C(); //This should go to Base.Status = 3
            d.Method(x);
            if (Base.Status == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovr012.ovr012
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual void Method(int x)
        {
            Base.Status = 1;
        }

        public virtual void Method(long y)
        {
            Base.Status = 2;
        }
    }

    public class Derived : Base
    {
        public override void Method(long x)
        {
            Base.Status = 3;
        }
    }

    public class FurtherDerived : Derived
    {
        public override void Method(int y)
        {
            Base.Status = 4;
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
            dynamic d = new FurtherDerived();
            int x = 3;
            d.Method(x);
            if (Base.Status == 4) //We should pick the second method
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovrdynamic001.ovrdynamic001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(object x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(dynamic x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            C x = new C(); //This should go to Base.Status = 3
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Twoclass2methods.ovrdynamic002.ovrdynamic002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual void Method(dynamic x)
        {
            Base.Status = 1;
        }
    }

    public class Derived : Base
    {
        public override void Method(object x)
        {
            Base.Status = 2;
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
            dynamic d = new Derived();
            C x = new C(); //This should go to Base.Status = 3
            d.Method(x);
            if (Base.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}
