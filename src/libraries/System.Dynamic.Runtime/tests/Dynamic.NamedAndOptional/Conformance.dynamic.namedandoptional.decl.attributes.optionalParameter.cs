// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt01.opt01
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
        [Optional]
        dynamic i)
        {
            if (i == 0)
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
            int? a = 0;
            return p.Foo(a);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt01a.opt01a
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
        private const int i = 5;
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
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt02.opt02
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
        [Optional]
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt02a.opt02a
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
        [Optional]
        int ? i)
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
            dynamic p = new Parent();
            return p.Foo(2);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt03.opt03
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;
    using System;

    public class Parent
    {
        public int Foo(int j, [Optional]
        dynamic i)
        {
            if (j == 2 && i == Type.Missing)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt03a.opt03a
{
    // <Area>Use of Optional Parameters</Area>
    // <Title>Optional Parameters declared with Attributes</Title>
    // <Description>Optional Parameters declared with Attributes</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(int? j, [Optional]
        int ? i)
        {
            if (j == 2 && i == null)
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
            dynamic p = new Parent();
            return p.Foo(2);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt04.opt04
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
        [Optional]
        dynamic j, [Optional]
        dynamic i)
        {
            if (j == 2 && i == System.Type.Missing)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt04a.opt04a
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
        [Optional]
        int ? j, [Optional]
        int ? i)
        {
            if (j == 2 && i == null)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt07.opt07
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
        [Optional]
        dynamic j, dynamic i)
        {
            if (j == System.Type.Missing && i == 2)
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
            return p.Foo(i: 2);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt07a.opt07a
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
        [Optional]
        int ? j, int? i)
        {
            if (j == null && i == 2)
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
            return p.Foo(i: 2);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt08.opt08
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
        [Optional]
        dynamic j, int i)
        {
            if (j == System.Type.Missing && i == 0)
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
            return p.Foo(i: 0);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt08a.opt08a
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
        [Optional]
        int ? j, int? i)
        {
            if (j == null && i == 0)
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
            return p.Foo(i: 0);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt09.opt09
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.attributes.optionalParameter.opt09a.opt09a
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
        [Optional]
        string i)
        {
            if (i == null)
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
}
