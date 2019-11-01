// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.cnstrctor01.cnstrctor01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a constructor with OP</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public Parent(dynamic i = null)
        {
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.cnstrctor01a.cnstrctor01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a constructor with OP</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public Parent(dynamic i = default(object))
        {
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
            int? a = null;
            Parent p = new Parent(a);
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.cnstrctor02.cnstrctor02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a constructor with OP</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public Parent(dynamic i = default(dynamic), dynamic j = null)
        {
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.cnstrctor04.cnstrctor04
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a constructor with OP</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public Parent(dynamic i, int j = 1)
        {
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
            Parent p = new Parent(i: 1);
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.cnstrctor04a.cnstrctor04a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a constructor with OP</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public Parent(dynamic i, int j = 1)
        {
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
            dynamic i = 1;
            dynamic p = new Parent(i, j: i);
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl01.decl01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x = null, dynamic y = null)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl01a.decl01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, int? y = 1)
        {
            if (x == null && y == 1)
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
            dynamic x = default(int?);
            dynamic p = new Parent();
            return p.Foo(x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl03a.decl03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x, int? y = 1)
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
            int? x = 2;
            Parent p = new Parent();
            return p.Foo(x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl05.decl05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. multiple optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int z, dynamic x = default(dynamic), dynamic y = default(object))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl05a.decl05a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. multiple optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? z, int x = 2, int y = 1)
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
            dynamic z = 1;
            Parent p = new Parent();
            return p.Foo(z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl06a.decl06a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. Expressions used</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl07a.decl07a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. Max int</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl09a.decl09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl12.decl12
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl12a.decl12a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl13.decl13
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional params specified via opt attribute</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        dynamic i, [Optional]
        dynamic j, [Optional]
        dynamic f, [Optional]
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl13a.decl13a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional params specified via opt attribute</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl14a.decl14a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private const int x = 1;
        public int Foo(int? z = x)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl16a.decl16a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private const int x = 1;
        public int Foo(bool? z = true)
        {
            if (z.Value)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl16b.decl16b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        private const int x = 1;
        public int Foo(bool z = true)
        {
            if (z)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl18a.decl18a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl19a.decl19a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.decl20a.decl20a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.generic01.generic01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent<T>
        where T : class
    {
        public int Foo(T t = null)
        {
            if (t == null)
                return 0;
            return 1;
        }
    }

    public class Foo
    {
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
            dynamic p = new Parent<Foo>();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.generic02.generic02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description> Declaration of OPs with constant values</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent<T>
    {
        public int Foo(T t = default(T))
        {
            if (t == null)
                return 0;
            return 1;
        }
    }

    public class Foo
    {
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
            dynamic p = new Parent<Foo>();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer02.indexer02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, dynamic index2 = default(dynamic)]
        {
            get
            {
                return index2 ?? 1 - index;
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
            Parent p = new Parent();
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer02a.indexer02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int? index = 1, int? index2 = 1]
        {
            get
            {
                return (int)index2 - 1;
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
            dynamic d = 1;
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer03a.indexer03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int? index = 1, long? index2 = 1]
        {
            get
            {
                return (int)(index2 - 1);
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
            dynamic d = 1;
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer05.indexer05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, dynamic index2 = null]
        {
            get
            {
                return index2 == null ? 0 : 1;
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
            Parent p = new Parent();
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer05a.indexer05a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int? index = 1, string index2 = "test"]
        {
            get
            {
                return index2 == "test" ? 0 : 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer06a.indexer06a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct S
    {
        public int x;
    }

    public class Parent
    {
        public int this[int? index = 1, S s = default(S)]
        {
            get
            {
                return s.x == 0 ? 0 : 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer07a.indexer07a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class S
    {
    }

    public class Parent
    {
        public int this[int? index = 1, S s = null]
        {
            get
            {
                return s == null ? 0 : 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer08.indexer08
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, int? s = null]
        {
            get
            {
                return s == null ? 0 : 1;
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
            Parent p = new Parent();
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer08a.indexer08a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int? index = 1, int? s = null]
        {
            get
            {
                return s == null ? 0 : 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.indexer09a.indexer09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int? index = 1, int? s = 0]
        {
            get
            {
                return s == 0 ? 0 : 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms02.prms02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Optional before params should be allowed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x = default(dynamic), params object[] array)
        {
            if (x == null)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms02a.prms02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Optional before params should be allowed</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, params object[] array)
        {
            if (x == 2)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms08.prms08
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x = null, params dynamic[] array)
        {
            x = 2;
            if (x == 2)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms08a.prms08a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, params object[] array)
        {
            x = 2;
            if (x == 2)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms09.prms09
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x = null, params dynamic[] array)
        {
            if (x == 1 && array.Length == 3)
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
            return p.Foo(1, 2, 3, 4);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms09a.prms09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, params object[] array)
        {
            x = 2;
            if (array == null)
                return 1;
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
            dynamic d1 = 1;
            dynamic d2 = 2;
            dynamic d3 = 3;
            dynamic d4 = 4;
            dynamic p = new Parent();
            return p.Foo(d1, d2, d3, d4);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms10a.prms10a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, params object[] array)
        {
            x = 2;
            if (x == 2)
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms11.prms11
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(params dynamic[] array)
        {
            if (array.Length == 0)
                return 1;
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
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms11a.prms11a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(params object[] array)
        {
            if (array.Length == 0)
                return 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p.Foo(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms12.prms12
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic x = null, params dynamic[] array)
        {
            if (x == 1 && (array == null || array.Length == 0))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.declaration.prms12a.prms12a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters
    // Params can't be defaulted</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? x = 2, params object[] array)
        {
            x = 2;
            if (array == null)
                return 1;
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
            dynamic d = 1;
            dynamic p = new Parent();
            return p.Foo(d);
        }
    }
    //</Code>
}
