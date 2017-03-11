// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested01.nested01
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 18)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(Bar2(Bar1())));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested01a.nested01a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 18)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i: Bar3(Bar2(Bar1())));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested01b.nested01b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 18)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(dynamic j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(Bar2(Bar1())));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested02.nested02
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(int k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(Bar1(Bar2(0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested02a.nested02a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(int k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(dynamic j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i: Bar3(Bar1(Bar2(0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested02b.nested02b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(Bar1(Bar2(0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested03.nested03
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(int k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(k: Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested03a.nested03a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(dynamic j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(k: Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested03b.nested03b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(dynamic j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(k: Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested03c.nested03c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(int k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            dynamic i = 0;
            return p.Foo(i: Bar3(j: Bar1(k: Bar2(i: i))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested05.nested05
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(int k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested05a.nested05a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested05b.nested05b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(int i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2(int i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3(int j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar3(j: Bar1(Bar2(i: 0))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.nested05c.nested05c
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic nesting of functions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic Foo(dynamic i)
        {
            if (i == 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1(dynamic k)
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2(dynamic i)
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3(dynamic j)
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 0;
            return p.Foo(i: Bar3(j: Bar1(Bar2(i: i))));
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order01.order01
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 28)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar1(), j: Bar2(), k: Bar3());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order01a.order01a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 28)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i: Bar1(), j: Bar2(), k: Bar3());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order01b.order01b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 28)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(i: Bar1(), j: Bar2(), k: Bar3());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order02.order02
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 33)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order02a.order02a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, dynamic k)
        {
            if (i + j + k == 33)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order02b.order02b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, dynamic k)
        {
            if (i + j + k == 33)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order03.order03
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 11 + 12 + 48)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(k: Bar3(), i: Bar1(), j: Bar2());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order03a.order03a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i, int j, int k)
        {
            if (i + j + k == 11 + 12 + 48)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(k: Bar3(), i: Bar1(), j: Bar2());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order03b.order03b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i, int j, int k)
        {
            if (i + j + k == 11 + 12 + 48)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(k: Bar3(), i: Bar1(), j: Bar2());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order04.order04
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 100)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(k: Bar3(), j: Bar2(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order04a.order04a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, dynamic j, int k)
        {
            if (i + j + k == 100)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(k: Bar3(), j: Bar2(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order04b.order04b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, dynamic j, int k)
        {
            if (i + j + k == 100)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(k: Bar3(), j: Bar2(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order05.order05
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j, int k)
        {
            if (i + j + k == 18 + 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static int Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static int Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static int Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order05a.order05a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i, dynamic j, dynamic k)
        {
            if (i + j + k == 18 + 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.executeOrder.order05b.order05b
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Ensuring execution order is correct</Title>
    // <Description>Basic Execution order</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i, dynamic j, dynamic k)
        {
            if (i + j + k == 18 + 15)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        public static int order1 = 1;
        public static dynamic Bar1()
        {
            order1 = order1 + 1;
            return order1;
        }

        public static dynamic Bar2()
        {
            order1 = order1 * 4;
            return order1;
        }

        public static dynamic Bar3()
        {
            order1 = order1 + 10;
            return order1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(j: Bar2(), k: Bar3(), i: Bar1());
        }
    }
    //</Code>
}
