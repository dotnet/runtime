// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.extension02.extension02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an Extension method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extension
    {
        public static int Foo(this Parent p, dynamic x = null)
        {
            return 0;
        }
    }

    public class Parent
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.extension02a.extension02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an Extension method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extension
    {
        public static int Foo(this Parent p, int? x = 1)
        {
            return 0;
        }
    }

    public class Parent
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
            dynamic p = new Parent();
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Parent", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.extension02b.extension02b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of an Extension method with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extension
    {
        public static int Foo(this Parent p, int? x = 1)
        {
            return x == null ? 0 : 1;
        }
    }

    public class Parent
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
            dynamic p = new Parent();
            dynamic d = null;
            try
            {
                p.Foo(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Parent", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.partial04b.partial04b
{
    public partial class Parent
    {
        partial void Foo(int? i = 0);
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.partial04b.partial04b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a Partial class with OPs</Description>
    // <Expects status=success></Expects>
    // <Code>
    public partial class Parent
    {
        public Parent()
        {
            TestOk = false;
        }

        public bool TestOk
        {
            get;
            set;
        }

        public void FooTest()
        {
            Foo();
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
            p.FooTest();
            if (p.TestOk)
                return 1;
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array01.array01
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic[] i = null)
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
            Parent p = new Parent();
            return p.Foo();
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array01a.array01a
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int?[] i = null)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array01b.array01b
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int?[] i = null)
        {
            if (i[0] == null && i[1] == 1)
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
            dynamic d = new int?[]
            {
            null, 1
            }

            ;
            dynamic p = new Parent();
            return p.Foo(d);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array03.array03
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic[] i = null)
        {
            if (i[1] == 2)
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
            return p.Foo(new object[]
            {
            1, 2, 3
            }

            );
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array03a.array03a
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int?[] i = null)
        {
            if (i[1] == 2)
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
            dynamic d = new int?[]
            {
            1, 2, 3
            }

            ;
            return p.Foo(d);
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array04.array04
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(dynamic[] i = null)
        {
            if (i[1] == 2)
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
            return p.Foo(i: new object[]
            {
            1, 2, 3
            }

            );
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.other.array04a.array04a
{
    // <Area>Use of optional Params</Area>
    // <Title>Use of optional arrays</Title>
    // <Description>should be able to have a default array</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int?[] i = null)
        {
            if (i[1] == 2)
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
            dynamic d = new int?[]
            {
            1, 2, null
            }

            ;
            return p.Foo(i: d);
        }
    }
}
