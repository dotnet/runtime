// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named01a.named01a
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            return tf.Foo(x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named01b.named01b
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived tf = new Derived();
            dynamic d = 2;
            return tf.Foo(x: d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named01c.named01c
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            dynamic d = 2;
            return tf.Foo(x: d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named02a.named02a
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x, int y)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            return tf.Foo(y: 1, x: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named02b.named02b
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x, int y)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived tf = new Derived();
            dynamic d1 = 1;
            dynamic d2 = 2;
            return tf.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named02c.named02c
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with named parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x, int y)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            dynamic d1 = 1;
            dynamic d2 = 2;
            return tf.Foo(y: d1, x: d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named03a.named03a
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with and incorrect parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            try
            {
                tf.Foo(Invalid: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "Invalid");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.named03c.named03c
{
    // <Area>Named Parameters</Area>
    // <Title> Basic Named Parameter</Title>
    // <Description>Basic testing of a simple function with and incorrect parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            dynamic d = 2;
            try
            {
                tf.Foo(Invalid: d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "Invalid");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.optional03.optional03
{
    // <Area>Optional Parameters</Area>
    // <Title> Basic Optional Parameter</Title>
    // <Description>Basic testing of a simple function with non-optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x)
        {
            if (x == 2)
                return 1;
            else
                return 0;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            try
            {
                tf.Foo();
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.optional02a.optional02a
{
    // <Area>Optional Parameters</Area>
    // <Title> Basic Optional Parameter</Title>
    // <Description>Basic testing of a simple function with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x = 2)
        {
            if (x == 2)
                return 1;
            else
                return 0;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            return tf.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.optional02b.optional02b
{
    // <Area>Optional Parameters</Area>
    // <Title> Basic Optional Parameter</Title>
    // <Description>Basic testing of a simple function with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x = 2)
        {
            if (x == 2)
                return 1;
            else
                return 0;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived tf = new Derived();
            dynamic d = 1;
            return tf.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.optional02c.optional02c
{
    // <Area>Optional Parameters</Area>
    // <Title> Basic Optional Parameter</Title>
    // <Description>Basic testing of a simple function with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x = 2)
        {
            if (x == 2)
                return 1;
            else
                return 0;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            dynamic d = 1;
            return tf.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.basic.optional01.optional01
{
    // <Area>Optional Parameters</Area>
    // <Title> Basic Optional Parameter</Title>
    // <Description>Basic testing of a simple function with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Derived
    {
        public int Foo(int x = 2)
        {
            if (x == 2)
                return 0;
            else
                return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            return tf.Foo();
        }
    }
    //</Code>
}
