// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate01.dlgate01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(dynamic i = null);
    public class Test
    {
        public static int Foo2(dynamic i)
        {
            return i ?? 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Foo2;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate01a.dlgate01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 0);
    public class Test
    {
        public static int Foo2(int? i)
        {
            return (int)i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Foo2;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate02a.dlgate02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(int? i);
    public class Test
    {
        private static int Bar(int? i = 1)
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
            dynamic f = (Foo)Bar;
            try
            {
                f();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadDelArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate03.dlgate03
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(dynamic i = default(dynamic));
    public class Test
    {
        private static int Bar(dynamic i = default(object))
        {
            return i ?? 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Bar;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate03a.dlgate03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 0);
    public class Test
    {
        private static int Bar(int? i = 1)
        {
            return (int)i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Bar;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate04a.dlgate04a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 2);
    public class Test
    {
        private static int Bar(int? i = 1)
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Bar;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate05.dlgate05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(dynamic i = null);
    public class Test
    {
        private static int Bar(dynamic i = null)
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Bar;
            return f(2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate05a.dlgate05a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 3);
    public class Test
    {
        private static int Bar(int? i = 1)
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 2;
            dynamic f = (Foo)Bar;
            return f(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.dlgate06a.dlgate06a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 3, int? j = 5, string t = "test");
    public class Test
    {
        private static int Bar(int? i = 1, int? j = 4, string t = "test2")
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 2;
            dynamic f = (Foo)Bar;
            return f(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.evnt01.evnt01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters
    //Optionality should have no effect on delegate assignment</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(dynamic i = null, dynamic j = null, string t = "test");
    public class Test
    {
        private static event Foo even;
        private static int Bar(dynamic i = null, dynamic j = null, string t = "test2")
        {
            if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            even += Bar;
            return even(2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.evnt01a.evnt01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters
    //Optionality should have no effect on delegate assignment</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 2, int? j = 5, string t = "test");
    public class Test
    {
        private static event Foo even;
        private static int Bar(int? i = 1, int? j = 4, string t = "test2")
        {
            if (i == 1)
                return 1;
            else if (i == 3)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 3;
            even += Bar;
            return even(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.evnt02.evnt02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters
    //Optionality should have no effect on delegate assignment</Description>
    // <Expects status=success></Expects>
    // <Code>
    internal delegate int Foo(dynamic i = null, dynamic j = default(dynamic), dynamic t = default(object));
    public class Test
    {
        private static event Foo even;
        private static int Bar(dynamic i = default(dynamic), dynamic j = null, dynamic t = default(object))
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        private static int Bar2(dynamic i, dynamic j = default(object), dynamic t = default(dynamic))
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            even += Bar;
            even += Bar2;
            return even(2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.evnt02a.evnt02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters
    //Optionality should have no effect on delegate assignment</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int? i = 2, int? j = 5, string t = "test");
    public class Test
    {
        private static event Foo even;
        private static int Bar(int? i = 1, int? j = 4, string t = "test2")
        {
            if (i == 10 && j == 20)
                return 0;
            return 1;
        }

        private static int Bar2(int? i, int? j = 4, string t = "test2")
        {
            if (i == 10 && j == 20)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d1 = 10;
            dynamic d2 = 20;
            even += Bar;
            even += Bar2;
            return even(d1, d2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.dlgate.evnt03a.evnt03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of delegates Optional Params</Title>
    // <Description>Simple Declaration of a delegate with optional parameters
    //Optionality should have no effect on delegate assignment</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,22\).*CS0067</Expects>
    public delegate int Foo(int? i = 3, int? j = 5, string t = "test");
    public delegate int Foo2(int? i, int? j, string t);
    public class Test
    {
        private static event Foo even;
        private static int Bar(int? i = 1, int? j = 4, string t = "test2")
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        private static int Bar2(int? i = 5, int? j = 3, string t = "")
        {
            if (i == 1)
                return 1;
            else if (i == 2)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f2 = (Foo2)Bar2;
            even += Bar;
            try
            {
                even += f2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "Foo2", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}
