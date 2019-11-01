// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct01.strct01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(dynamic x = null, dynamic y = default(dynamic))
        {
            if (x == null && y == null)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct01a.strct01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(int? x = 2, int? y = 1)
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
            dynamic d = 1;
            return p.Foo(y: d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct03.strct03
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(dynamic x, dynamic y = null)
        {
            if (x == 2 && y == null)
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
            return p.Foo(2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct03a.strct03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(int? x, int? y = 1)
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
            dynamic d = 2;
            return p.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct05.strct05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. multiple optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(dynamic z, dynamic x = null, dynamic y = null)
        {
            if (z == 1 && x == null && y == null)
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
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct05a.strct05a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. multiple optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(int? z, int? x = 2, int? y = 1)
        {
            if (z == 1 && x == 2 && y == 1)
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
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct06a.strct06a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. Expressions used</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(int? z = 1 + 1)
        {
            if (z == 2)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct07a.strct07a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. Max int</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(int? z = 2147483647)
        {
            if (z == 2147483647)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct09a.strct09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(long? z = (long)1)
        {
            if (z == 1)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct12.strct12
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public struct Parent
    {
        public int Foo(
        [Optional]
        dynamic i)
        {
            if (i == System.Type.Missing)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct12a.strct12a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public struct Parent
    {
        public int Foo(
        [Optional]
        int ? i)
        {
            if (i == null)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct13.strct13
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public struct Parent
    {
        public int Foo(
        [Optional]
        dynamic i, [Optional]
        long j, [Optional]
        float f, [Optional]
        dynamic d)
        {
            if (d == System.Type.Missing)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct13a.strct13a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public struct Parent
    {
        public int Foo(
        [Optional]
        int ? i, [Optional]
        long ? j, [Optional]
        float ? f, [Optional]
        decimal ? d)
        {
            if (d == null)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct14a.strct14a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        private const int x = 1;
        public int Foo(long? z = x)
        {
            if (z == 1)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct18a.strct18a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        private const string x = "test";
        public int Foo(string z = x)
        {
            if (z == "test")
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct19a.strct19a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        private const bool x = true;
        public int Foo(bool? z = x)
        {
            if ((bool)z)
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
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.strct.strct20a.strct20a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct Parent
    {
        public int Foo(string z = "test", int? y = 3)
        {
            if (z == "test" && y == 3)
                return 1;
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
                p.Foo(3, "test");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(string, int?)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}
