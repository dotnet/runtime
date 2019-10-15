// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload01.overload01
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0)
        {
            return 1;
        }

        public int Foo()
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload01a.overload01a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null)
        {
            return 1;
        }

        public int Foo()
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload01b.overload01b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = default(dynamic))
        {
            return 1;
        }

        public int Foo()
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload02.overload02
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0)
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
            dynamic p = new Parent();
            try
            {
                p.Foo(); //No CS0121
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(int, int)", "Parent.Foo(int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload02b.overload02b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <RelatedBugs></RelatedBugs>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, dynamic j = default(dynamic))
        {
            return 1;
        }

        public int Foo(dynamic i = default(dynamic))
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
            dynamic p = new Parent();
            try
            {
                p.Foo(); //No CS0121
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(object, object)", "Parent.Foo(object)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload03.overload03
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Should be ambiguous</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0)
        {
            return 1;
        }

        public int Foo(string s = "")
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
            dynamic p = new Parent();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(string)", "Parent.Foo(int, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload03b.overload03b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Should be ambiguous</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, dynamic j = null)
        {
            return 1;
        }

        public int Foo(dynamic i = default(dynamic))
        {
            return 1;
        }

        public int Foo(string s = "")
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
            dynamic p = new Parent();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(string)", "Parent.Foo(object, object)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload04.overload04
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0)
        {
            return 1;
        }

        public int Foo()
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload04a.overload04a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i = null, dynamic j = null)
        {
            return 1;
        }

        public dynamic Foo(dynamic i = null)
        {
            return 1;
        }

        public dynamic Foo()
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload04b.overload04b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i = null, dynamic j = null)
        {
            return 1;
        }

        public dynamic Foo(dynamic i = null)
        {
            return 1;
        }

        public dynamic Foo()
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload05.overload05
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Ambiguous on the second param</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0, string s = "")
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
            dynamic p = new Parent();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(int, int)", "Parent.Foo(int, string)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload05b.overload05b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Ambiguous on the second param</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i = null, int j = 0)
        {
            return 1;
        }

        public dynamic Foo(dynamic i = default(dynamic), string s = "")
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
            dynamic p = new Parent();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "Parent.Foo(object, int)", "Parent.Foo(object, string)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload06.overload06
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named params</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0)
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload06a.overload06a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named params</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 1;
        }

        public int Foo(int i = 0)
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
            Parent p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload06b.overload06b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named params</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 1;
        }

        public int Foo(int i = 0)
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload06c.overload06c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named params</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }

        public int Foo(int i = 0)
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload07.overload07
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int z = 1)
        {
            return 1;
        }

        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0)
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
            dynamic p = new Parent();
            return p.Foo(i: 2, j: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload07a.overload07a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i, dynamic j, dynamic z = null)
        {
            return 1;
        }

        public dynamic Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public dynamic Foo(int i = 0)
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
            Parent p = new Parent();
            return p.Foo(i: 2, j: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload07b.overload07b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i, dynamic j, dynamic z = default(dynamic))
        {
            return 1;
        }

        public dynamic Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public dynamic Foo(int i = 0)
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
            dynamic p = new Parent();
            return p.Foo(i: 2, j: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload07c.overload07c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int z = 1)
        {
            return 1;
        }

        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0)
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
            Parent p = new Parent();
            dynamic i = 2;
            dynamic j = 1;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload09.overload09
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long i)
        {
            return 0;
        }
        //public int Foo(int i)  {return 1;}
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload09a.overload09a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(long i)
        {
            return 0;
        }
        //public int Foo(int i)  {return 1;}
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
            Parent p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload09b.overload09b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(long i)
        {
            return 0;
        }
        //public int Foo(int i)  {return 1;}
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload09c.overload09c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long i)
        {
            return 0;
        }
        //public int Foo(int i)  {return 1;}
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload10.overload10
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long i)
        {
            return 1;
        }

        public int Foo(int i)
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload10a.overload10a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(long i)
        {
            return 1;
        }

        public int Foo(int i)
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
            Parent p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload10b.overload10b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(long i)
        {
            return 1;
        }

        public int Foo(int i)
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
            dynamic p = new Parent();
            return p.Foo(i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload10c.overload10c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution with named. This is here mostly as a regression test</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long i)
        {
            return 1;
        }

        public int Foo(int i)
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload11.overload11
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload11a.overload11a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload11b.overload11b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload12.overload12
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload12a.overload12a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(params dynamic[] arr)
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload12b.overload12b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <RelatedBugs></RelatedBugs>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(params dynamic[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload13.overload13
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(i: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload13a.overload13a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, int j = 0)
        {
            return 0;
        }

        public dynamic Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            // select 2nd Foo as the first type is int
            return p.Foo(i: 0) - 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload13b.overload13b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, int j = 0)
        {
            return 1;
        }

        public dynamic Foo(int i = 0, params int[] arr)
        {
            return 0;
        } //we should pick this method
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
            dynamic p = new Parent();
            return p.Foo(i: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload13c.overload13c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic i = 0;
            return p.Foo(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload14.overload14
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload14a.overload14a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(dynamic i = null, params int[] arr)
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
            Parent p = new Parent();
            return p.Foo(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload14b.overload14b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(dynamic i = null, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload14c.overload14c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic j = 0;
            return p.Foo(j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload15.overload15
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload15a.overload15a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i = null, dynamic j = null)
        {
            return 1;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic d1 = 1;
            dynamic d2 = 'c';
            // the conversion from (int, char) to (int int) is better than
            // to (object, object), so the second Foo is picked.
            return p.Foo(d1, d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload15b.overload15b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i = null, dynamic j = null)
        {
            return 1;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload15c.overload15c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic i = 1;
            dynamic j = 2;
            return p.Foo(i, j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload16.overload16
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload16a.overload16a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(dynamic i = null, params dynamic[] arr)
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
            Parent p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload16b.overload16b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 0;
        }

        public int Foo(dynamic i = null, params dynamic[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload16c.overload16c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic j = 2;
            return p.Foo(1, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload17.overload17
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload17a.overload17a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, dynamic j = null)
        {
            return 0;
        }

        public dynamic Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload17b.overload17b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, dynamic j = null)
        {
            return 0;
        }

        public dynamic Foo(int i = 0, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload17c.overload17c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }

        public int Foo(int i = 0, params int[] arr)
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
            Parent p = new Parent();
            dynamic i = 1;
            dynamic j = 2;
            return p.Foo(i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload18.overload18
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }
    }

    public class Derived : Parent
    {
        public int Foo(int i = 0, params int[] arr)
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
            dynamic p = new Derived();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload18a.overload18a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, int j = 0)
        {
            return 0;
        }
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null, params int[] arr)
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
            Derived p = new Derived();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload18b.overload18b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i = null, int j = 0)
        {
            return 0;
        }
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null, params int[] arr)
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
            dynamic p = new Derived();
            return p.Foo(1, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload18c.overload18c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 0;
        }
    }

    public class Derived : Parent
    {
        public int Foo(int i = 0, params int[] arr)
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
            Derived p = new Derived();
            dynamic i = 1;
            dynamic j = 2;
            return p.Foo(i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload19.overload19
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public int Foo(int i = 0, params int[] arr)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload19a.overload19a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public dynamic Foo(int i = 0, params dynamic[] arr)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload19b.overload19b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, dynamic j = null)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public dynamic Foo(int i = 0, params dynamic[] arr)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload19c.overload19c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public int Foo(int i = 0, params int[] arr)
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
            dynamic i = 1;
            dynamic j = 2;
            return p.Foo(i, j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload20.overload20
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i = 0, int j = 0)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public int Foo(int i = 0, params int[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload20a.overload20a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, int j = 0)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null, params int[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload20b.overload20b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i = 0, int j = 0)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public int Foo(dynamic i = null, params int[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload21.overload21
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int j = 0)
        {
            return 0;
        }

        public int Foo(params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload21b.overload21b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic j = null)
        {
            return 0;
        }

        public dynamic Foo(params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload22.overload22
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int i = 0, params int[] arr)
        {
            return 1;
        }

        public int Foo(int i = 0, int j = 0)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload22a.overload22a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public dynamic Foo(int i = 0, params dynamic[] arr)
        {
            return 1;
        }

        public dynamic Foo(int i = 0, int j = 0)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload22b.overload22b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public dynamic Foo(int i = 0, params dynamic[] arr)
        {
            return 1;
        }

        public dynamic Foo(int i = 0, int j = 0)
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
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload22c.overload22c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int i = 0, params int[] arr)
        {
            return 1;
        }

        public int Foo(int i = 0, int j = 0)
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
            dynamic i = 1;
            dynamic j = 2;
            return p.Foo(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload23.overload23
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <Expects status=success></Expects>
    // <Code>

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            try
            {
                d.Foo(x: 1, y: "", z: "");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "C.Foo(int, string, string)", "C.Foo(string, int, string)");
                if (ret)
                    return 0;
            }

            return 1;
        }

        public void Foo(int x, string y, string z)
        {
        }

        public void Foo(string y, int x, string z)
        {
        }

        public void Foo(string z, string y, int? x)
        {
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload24.overload24
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Overload resolution of named params and optionals</Title>
    // <Description>Basic Overload resolution - based on overload resolution rules</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(object i, object j = null)
        {
            return 2;
        }

        public int Foo(int i = 0, params object[] arr)
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
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.overload.overload25.overload25
{
    // <Area>N&O</Area>
    // <Title>overload resolution</Title>
    // <Description>
    //   Overload resolution with generics and optional args
    // </Description>
    // <Related Bugs></Related Bugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public int Foo(int x, string y = null)
        {
            return 0;
        }

        public int Foo<T>(T x)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(1, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new A();
            return d.Foo(0);
        }
    }
    //</Code>
}
