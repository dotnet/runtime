// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit01.inherit01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic i = null)
        {
            return i ?? 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(dynamic i = default(dynamic))
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit01a.inherit01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 1)
        {
            return (int)i;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit02.inherit02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic i)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(dynamic i = null)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit02a.inherit02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i)
        {
            return (int)i;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit03a.inherit03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 0)
        {
            return (int)1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int? i)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit04.inherit04
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return 0;
        }
    }

    public class Derived : Parent
    {
        public new dynamic Foo(dynamic i)
        {
            return 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit04a.inherit04a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int? i)
        {
            return (int)1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit05.inherit05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new dynamic Foo(dynamic i = null)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit05a.inherit05a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 1)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int? i = 0)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit06.inherit06
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public override dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override dynamic Foo(dynamic i = null)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit06a.inherit06a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 0)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public override int Foo(int? i = 2)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit07.inherit07
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public virtual new dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override dynamic Foo(dynamic i = null)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit07a.inherit07a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 1)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public virtual new int Foo(int? i = 20)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit08.inherit08
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public override dynamic Foo(dynamic i = null)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public new dynamic Foo(dynamic i = null)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit08a.inherit08a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int? i = 1)
        {
            return 1;
        }
    }

    public class Child : Parent
    {
        public override int Foo(int? i = 1)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public new int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit09.inherit09
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(dynamic i = null);
    }

    public class Derived : Parent
    {
        public dynamic Foo(dynamic i = null)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit09a.inherit09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public class Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit10.inherit10
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic i);
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit10a.inherit10a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i);
    }

    public class Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit11a.inherit11a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public class Derived : Parent
    {
        public int Foo(int? i)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit12a.inherit12a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS0109</Expects>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public class Derived : Parent
    {
        public new int Foo(int? i)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit13.inherit13
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(int i = 1);
    }

    public class Derived : Parent
    {
        public dynamic Foo(int i = 0)
        {
            return i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit13a.inherit13a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public class Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit14.inherit14
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(int i = 1);
    }

    public class Child : Parent
    {
        public virtual dynamic Foo(int i = 0)
        {
            return i - 2;
        }
    }

    public class Derived : Child
    {
        public override dynamic Foo(int i = 0)
        {
            return i + 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Child c = new Child();
            Derived p = new Derived();
            return p.Foo() + c.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit14a.inherit14a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public class Child : Parent
    {
        public virtual int Foo(int? i = 0)
        {
            return (int)i - 2;
        }
    }

    public class Derived : Child
    {
        public override int Foo(int? i = 0)
        {
            return (int)i + 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new Child();
            dynamic p = new Derived();
            return p.Foo() + c.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit15.inherit15
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,29\).*CS0109</Expects>
    public interface Parent
    {
        dynamic Foo(int i = 1);
    }

    public class Child : Parent
    {
        public virtual new dynamic Foo(int i = 1)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override dynamic Foo(int i = 0)
        {
            return i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit15a.inherit15a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,25\).*CS0109</Expects>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public class Child : Parent
    {
        public virtual new int Foo(int? i = 1)
        {
            return 1;
        }
    }

    public class Derived : Child
    {
        public override int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit19.inherit19
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(dynamic i = null);
    }

    public struct Derived : Parent
    {
        public dynamic Foo(dynamic i = null)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit19a.inherit19a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public struct Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit20.inherit20
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(dynamic i = null);
    }

    public struct Derived : Parent
    {
        public dynamic Foo(dynamic i)
        {
            return i ?? 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit20a.inherit20a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public struct Derived : Parent
    {
        public int Foo(int? i)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p1 = new Derived();
            dynamic p = p1;
            return ((Parent)p).Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit21.inherit21
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        dynamic Foo(dynamic i = null);
    }

    public class Child : Parent
    {
        public virtual dynamic Foo(dynamic i = null)
        {
            return i ?? 0 - 2;
        }
    }

    public class Derived : Child
    {
        public override dynamic Foo(dynamic i = null)
        {
            return i ?? 0 + 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Child c = new Child();
            Derived p = new Derived();
            return p.Foo() + c.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit21a.inherit21a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public class Child : Parent
    {
        public virtual int Foo(int? i = 0)
        {
            return (int)i - 2;
        }
    }

    public class Derived : Child
    {
        public override int Foo(int? i = 0)
        {
            return (int)i + 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new Child();
            dynamic p = new Derived();
            return p.Foo() + c.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit22.inherit22
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic i = null);
    }

    public struct Derived : Parent
    {
        public int Foo(dynamic i = null)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Derived();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit22a.inherit22a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public struct Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p1 = new Derived();
            dynamic p = p1;
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit23.inherit23
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic i = null);
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null)
        {
            return i - 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Derived();
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit23a.inherit23a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 1);
    }

    public class Derived : Parent
    {
        public int Foo(int? i = 0)
        {
            return (int)i - 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p1 = new Derived();
            dynamic p = p1;
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.inheritance.inherit24a.inherit24a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a inheritance chain with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int? i = 0);
    }

    public struct Derived : Parent
    {
        public int Foo(int? i)
        {
            return i.Value;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}
