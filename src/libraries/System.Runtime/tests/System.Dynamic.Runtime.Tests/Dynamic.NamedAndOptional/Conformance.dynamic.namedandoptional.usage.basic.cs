// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic01.generic01
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            return p.Foo<int>(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic01a.generic01a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            dynamic d1 = 3;
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic01b.generic01b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            dynamic d1 = 3;
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic01c.generic01c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, dynamic y)
        {
            if (y == 2)
                return 0;
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
            return p.Foo<int>(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic01d.generic01d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic d1 = 3;
            dynamic d2 = 2;
            return Foo<int>(x: d1, y: d2);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic02.generic02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            return p.Foo<int>(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic02a.generic02a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            dynamic d1 = (short)3;
            dynamic d2 = 2;
            return p.Foo<short>(x: d1, y: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic02b.generic02b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
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
            dynamic d1 = (byte)3;
            dynamic d2 = 2;
            return p.Foo<byte>(y: d2, x: d1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic02c.generic02c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, dynamic y)
        {
            if (y == 2)
                return 0;
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
            return p.Foo(y: 2, x: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic02d.generic02d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo<T>(T x, int y)
        {
            if (y == 2)
                return 0;
            return 1;
        }

        public int Method<T>()
        {
            dynamic d1 = (ulong)3;
            dynamic d2 = 2;
            return Foo<ulong>(x: d1, y: d2);
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
            return p.Method<string>();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic03.generic03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x, int y)
        {
            if (y == 2)
                return x;
            return default(T);
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
            return p.Foo<int>(x: 1, y: 2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic03a.generic03a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x, int y)
        {
            if (y == 2)
                return x;
            return default(T);
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
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic03b.generic03b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x, int y)
        {
            if (y == 2)
                return x;
            return default(T);
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic03c.generic03c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x, dynamic y)
        {
            if (y == 2)
                return x;
            return default(T);
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
            return p.Foo<int>(x: 1, y: 2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic03d.generic03d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x, int y)
        {
            if (y == 2)
                return x;
            return default(T);
        }

        public int MyProperty
        {
            get
            {
                dynamic d1 = 1;
                dynamic d2 = 2;
                return Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
            }

            set
            {
            }
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
            return p.MyProperty;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic04.generic04
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x = default(T), int y = 1)
        {
            if (y == 2)
                return x;
            return default(T);
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
            return p.Foo<int>(x: 1, y: 2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic04a.generic04a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x = default(T), int y = 1)
        {
            if (y == 2)
                return x;
            return default(T);
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
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic04b.generic04b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x = default(T), int y = 1)
        {
            if (y == 2)
                return x;
            return default(T);
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic04c.generic04c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x = default(T), dynamic y = null)
        {
            if (y == 2)
                return x;
            return default(T);
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
            return p.Foo<int>(x: 1, y: 2) == 1 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.generic04d.generic04d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are generically typed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public T Foo<T>(T x = default(T), int y = 1)
        {
            if (y == 2)
                return x;
            return default(T);
        }

        public int this[object o]
        {
            get
            {
                dynamic d1 = 1;
                dynamic d2 = 2;
                return Foo<int>(x: d1, y: d2) == 1 ? 0 : 1;
            }
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
            return p[p];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage02.usage02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 2, int y = 1)
        {
            if (x == 2 && y == 1)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage02a.usage02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 2, int y = 1)
        {
            if (x == 2 && y == 1)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage02b.usage02b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 2, int y = 1)
        {
            if (x == 2 && y == 1)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage02c.usage02c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 2, dynamic y = default(dynamic))
        {
            if (x == 2 && y == 1)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage02d.usage02d
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 2, int y = 1)
        {
            if (x == 2 && y == 1)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic d1 = 1;
            dynamic d2 = 2;
            return Foo(y: d1, x: d2);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage03.usage03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 2 && y == 1)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage03a.usage03a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 2 && y == 1)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage03b.usage03b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 2 && y == 1)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage03c.usage03c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, dynamic y)
        {
            if (x == 2 && y == 1)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage03d.usage03d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 2 && y == 1)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic d1 = 1;
            dynamic d2 = 2;
            return Foo(y: d1, x: d2);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage04.usage04
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage04a.usage04a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage04b.usage04b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            return p.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage04c.usage04c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, int y, dynamic z = null)
        {
            if (x == 2 && y == 1 && z == null)
                return 0;
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
            return p.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage04d.usage04d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
            return 1;
        }

        public int MyProperty
        {
            get
            {
                dynamic d1 = 1;
                dynamic d2 = 2;
                return Foo(y: d1, x: d2);
            }
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
            return p.MyProperty;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage05.usage05
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
                p.Foo(y: 1, x: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "2");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage05b.usage05b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            try
            {
                p.Foo(y: d1, x: d2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "2");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage06.usage06
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
                p.Foo(1, 3, x: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage07.usage07
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
                p.Foo(1, y: 2, x: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage07b.usage07b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            try
            {
                p.Foo(1, y: d2, x: d2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage09.usage09
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
                p.Foo(1, z: 2, x: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage09b.usage09b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, int z)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            try
            {
                p.Foo(d1, z: d2, x: d2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage10.usage10
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            return p.Foo(z: 3, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage10a.usage10a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 3;
            dynamic d2 = 2;
            return p.Foo(z: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage10b.usage10b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 3;
            dynamic d2 = 2;
            return p.Foo(z: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage10c.usage10c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, dynamic y = null, dynamic z = null)
        {
            if (x == 2 && y == null && z == 3)
                return 0;
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
            return p.Foo(z: 3, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage10d.usage10d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
            return 1;
        }

        public int this[int i]
        {
            get
            {
                dynamic d1 = 3;
                dynamic d2 = 2;
                return Foo(z: d1, x: d2);
            }
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
            return p[0];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage12.usage12
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
                p.Foo(z: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "1");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage12b.usage12b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 1)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 2;
            try
            {
                p.Foo(z: d1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "1");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage13.usage13
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            return p.Foo(x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage13a.usage13a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 2;
            return p.Foo(x: d1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage13b.usage13b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
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
            dynamic d1 = 2;
            return p.Foo(x: d1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage13c.usage13c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, int y = 1, dynamic z = null)
        {
            if (x == 2 && y == 1 && z == null)
                return 0;
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
            return p.Foo(x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage13d.usage13d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 1, int z = 3)
        {
            if (x == 2 && y == 1 && z == 3)
                return 0;
            return 1;
        }

        public int Method<T>()
        {
            dynamic d1 = 2;
            return Foo(x: d1);
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
            return p.Method<object>();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage14.usage14
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x)
        {
            if (x == 3)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int C = 3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage14a.usage14a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x)
        {
            if (x == 3)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = 3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(x: s_C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage14b.usage14b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x)
        {
            if (x == 3)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = 3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: s_C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage14c.usage14c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x)
        {
            if (x == 3)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int C = 3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(x: C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage14d.usage14d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private static dynamic s_C = 3;
        public int Foo(int x)
        {
            if (x == 3)
                return 0;
            return 1;
        }

        public int Method()
        {
            return Foo(x: s_C);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage15.usage15
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x)
        {
            if (x)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const bool C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage15a.usage15a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x)
        {
            if (x)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(x: s_C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage15b.usage15b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x)
        {
            if (x)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: s_C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage15c.usage15c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x)
        {
            if (x)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const bool C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(x: C);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage15d.usage15d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private static dynamic s_C = true;
        public int Foo(bool x)
        {
            if (x)
                return 0;
            return 1;
        }

        public int Method()
        {
            return Foo(x: s_C);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage16.usage16
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const bool C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(true, y: C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage16a.usage16a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(true, y: s_C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage16b.usage16b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(true, y: s_C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage16c.usage16c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, dynamic y)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const bool C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(true, y: C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage16d.usage16d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private static dynamic s_C = true;
        public int Field = 10;
        public int Foo(bool x, bool y)
        {
            if (x && y)
                return 0;
            return 1;
        }

        public void Method()
        {
            Field = Foo(true, y: s_C ? true : false);
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
            p.Method();
            return p.Field;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage17.usage17
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y = true)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const bool C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage17a.usage17a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y = true)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(x: s_C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage17b.usage17b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool x, bool y = true)
        {
            if (x && y)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_C = true;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            return p.Foo(x: s_C ? true : false);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage17d.usage17d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private static dynamic s_C = true;
        public static int Field = 10;
        public int Foo(bool x, bool y = true)
        {
            if (x && y)
                return 0;
            return 1;
        }

        public void Method()
        {
            Field = Foo(x: s_C ? true : false);
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
            p.Method();
            return Parent.Field;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage18.usage18
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 3 && y == 2)
                return 0;
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
            int x = 3;
            int y = 2;
            dynamic p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage18a.usage18a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 3 && y == 2)
                return 0;
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
            dynamic x = 3;
            dynamic y = 2;
            Parent p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage18b.usage18b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 3 && y == 2)
                return 0;
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
            dynamic x = 3;
            dynamic y = 2;
            dynamic p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage18c.usage18c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, int y)
        {
            if (x == 3 && y == 2)
                return 0;
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
            int x = 3;
            int y = 2;
            Parent p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage18d.usage18d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic x = 3;
            dynamic y = 2;
            return Foo(x: x, y: y);
        }
    }

    public class Test
    {
        public int Field = 10;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            Test t = new Test()
            {
                Field = p.Method()
            }

            ;
            return t.Field;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage19.usage19
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
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
            int x = 3;
            int y = 2;
            dynamic p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage19a.usage19a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
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
            dynamic x = 3;
            dynamic y = 2;
            Parent p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage19b.usage19b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
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
            dynamic x = 3;
            dynamic y = 2;
            dynamic p = new Parent();
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage19d.usage19d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic x = 3;
            dynamic y = 2;
            return Foo(x: x, y: y);
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
            int[] array = new int[]
            {
            (int)p.Method()}

            ;
            return array[0];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage22.usage22
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    using X = X2;

    namespace X2
    {
        public class C
        {
            public static X.C get()
            {
                return new X.C();
            }
        }
    }

    public class Parent
    {
        public int Foo(X.C X)
        {
            if (X != null)
                return 0;
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
            return p.Foo(X: X::C.get());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage22a.usage22a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    using X = X2;

    namespace X2
    {
        public class C
        {
            public static dynamic get()
            {
                return new X.C();
            }
        }
    }

    public class Parent
    {
        public int Foo(X.C X)
        {
            if (X != null)
                return 0;
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
            return p.Foo(X: X::C.get());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage22b.usage22b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    using X = X2;

    namespace X2
    {
        public class C
        {
            public static dynamic get()
            {
                return new X.C();
            }
        }
    }

    public class Parent
    {
        public int Foo(X.C X)
        {
            if (X != null)
                return 0;
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
            return p.Foo(X: X::C.get());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage22c.usage22c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    using X = X2;

    namespace X2
    {
        public class C
        {
            public static X.C get()
            {
                return new X.C();
            }
        }
    }

    public class Parent
    {
        public int Foo(dynamic X)
        {
            if (X != null)
                return 0;
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
            return p.Foo(X: X::C.get());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage22d.usage22d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    using X = X2;

    namespace X2
    {
        public class C
        {
            public static dynamic get()
            {
                return new X.C();
            }
        }
    }

    public class Parent
    {
        public int Foo(X.C X)
        {
            if (X != null)
                return 0;
            return 1;
        }

        public int Method()
        {
            return Foo(X: X::C.get());
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage23.usage23
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j)
        {
            if (i == 5)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int i = 5;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool x = true;
            int y = 5;
            dynamic p = new Parent();
            return p.Foo(x ? y : 0, 1);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage23a.usage23a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j)
        {
            if (i == 5)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int i = 5;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = true;
            dynamic y = 5;
            Parent p = new Parent();
            return p.Foo(x ? y : 0, 1);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage23b.usage23b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j)
        {
            if (i == 5)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int i = 5;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = true;
            dynamic y = 5;
            dynamic p = new Parent();
            return p.Foo(x ? y : 0, 1);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage23c.usage23c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic i, dynamic j)
        {
            if (i == 5)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const int i = 5;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool x = true;
            int y = 5;
            Parent p = new Parent();
            return p.Foo(x ? y : 0, 1);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage23d.usage23d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters are non optional</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int i, int j)
        {
            if (i == 5)
                return 0;
            return 1;
        }

        public static int Method()
        {
            dynamic x = true;
            dynamic y = 5;
            dynamic p = new Parent();
            return p.Foo(x ? y : 0, 1);
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
            return Parent.Method();
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage24.usage24
{
    // <Title> No ambiguity when calling methods with N&O</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            int tests = 0, success = 0;
            var s = new Test();
            dynamic d = new Test();
            tests++;
            if (s.Foo(x: 1, y: "") == 2)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: "") == 2)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            return tests == success ? 0 : 1;
        }

        public int Foo(int x, object y)
        {
            return 1;
        }

        public int Foo<T, U>(U x, T y)
        {
            return 2;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage25.usage25
{
    // <Title> No ambiguity when calling methods with N&O</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            int tests = 0, success = 0;
            var s = new Test();
            dynamic d = new Test();
            tests++;
            if (s.Foo(x: 1, y: "") == 2)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: "") == 2)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            return tests == success ? 0 : 1;
        }

        public int Foo(int x, object y)
        {
            return 1;
        }

        public int Foo<T>(int x, T y)
        {
            return 2;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage26.usage26
{
    // <Title> No ambiguity when calling methods with N&O</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            int tests = 0, success = 0;
            var s = new Test();
            dynamic d = new Test();
            tests++;
            if (s.Foo(x: 1, y: "") == 1)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: "") == 1)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            return tests == success ? 0 : 1;
        }

        public int Foo<U>(U x, string y)
        {
            return 1;
        }

        public int Foo<U, T>(U x, T y)
        {
            return 2;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.basic.usage27.usage27
{
    // <Title> No ambiguity when calling methods with N&O</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            int tests = 0, success = 0;
            var s = new Test();
            dynamic d = new Test();
            tests++;
            if (s.Foo(x: 1, y: null) == 2)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: null) == 2)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            return tests == success ? 0 : 1;
        }

        public int Foo(int x, long? y)
        {
            System.Console.WriteLine(1);
            return 1;
        }

        public int Foo<T>(T x, T? y) where T : struct
        {
            return 2;
        }
    }
    // </Code>
}
