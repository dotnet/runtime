// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default01.default01
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1)]
        dynamic i)
        {
            if (i == 1)
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default02.default02
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1)]
        dynamic i)
        {
            if (i == 2)
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
            Parent p = new Parent();
            return p.Foo(2);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default02a.default02a
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1)]
        dynamic i)
        {
            if (i == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_i = 2;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(s_i);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default02b.default02b
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1)]
        dynamic i)
        {
            if (i == null)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const dynamic i = null;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default02c.default02c
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1)]
        dynamic i)
        {
            if (i == typeof(int?))
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static dynamic s_i = typeof(int?);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(s_i);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default04.default04
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue((long)2)]
        dynamic i)
        {
            if (i == 2)
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
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default05.default05
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("test")]
        dynamic i)
        {
            if (i == "test")
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
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default06.default06
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("test2")]
        dynamic i)
        {
            if (i == "test")
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
            return p.Foo("test");
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default06a.default06a
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("test2")]
        dynamic i)
        {
            if (i == null)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private const dynamic i = null;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(i);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default06b.default06b
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("test2")]
        dynamic i)
        {
            if (i.Length == 0 && i.GetType() == typeof(int[]))
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
            Parent p = new Parent();
            return p.Foo(new int[0]);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default06c.default06c
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("test2")]
        dynamic i)
        {
            if (i == 10)
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
            return p.Foo(10);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default07.default07
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(1 + 1)]
        dynamic i)
        {
            if (i == 2)
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default09.default09
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        dynamic i, [Optional, DefaultParameterValue("boo")]
        string str)
        {
            if (i == "foo" && str == "boo")
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default09a.default09a
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(10)]
        dynamic i, [Optional, DefaultParameterValue("boo")]
        dynamic str)
        {
            if (i == null && str == null)
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
            return p.Foo(null, null);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default09b.default09b
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(19,14\).*CS0414</Expects>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        dynamic i, [Optional, DefaultParameterValue(null)]
        dynamic str)
        {
            if (i == 5 && str == null)
                return 0;
            return 1;
        }
    }

    public class Test
    {
        private static int? s_i = 5;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Parent p = new Parent();
            return p.Foo(5);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default10.default10
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        dynamic i, [Optional, DefaultParameterValue("boo")]
        string str)
        {
            if (i == 10 && str == "bar")
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
            Parent p = new Parent();
            return p.Foo(10, "bar");
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default10a.default10a
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        string i, [Optional, DefaultParameterValue("boo")]
        dynamic str)
        {
            if (i == "test" && str == typeof(int))
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
            return p.Foo("test", typeof(int));
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default10b.default10b
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        dynamic i, [Optional, DefaultParameterValue("boo")]
        dynamic str)
        {
            if (i == "test" && str == "bar")
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
            Parent p = new Parent();
            return p.Foo("test", "bar");
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.defaultParameter.default10c.default10c
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue("foo")]
        string i, [Optional, DefaultParameterValue("boo")]
        string str)
        {
            if (i == "test" && str == "bar")
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
            return p.Foo("test", "bar");
        }
    }
}
