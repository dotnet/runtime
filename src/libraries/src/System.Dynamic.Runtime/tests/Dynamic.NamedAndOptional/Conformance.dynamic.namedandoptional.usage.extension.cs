// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext01.ext01
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext01a.ext01a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, dynamic i = null)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext02.ext02
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            if (i == 1)
                return 0;
            return 1;
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
                p.Foo(i: 1);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext02a.ext02a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, dynamic i = null)
        {
            if (i == 1)
                return 0;
            return 1;
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
            return p.Foo(i: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext04.ext04
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            if (i == 1)
                return 0;
            return 1;
        }

        public static int Foo(this Parent p, int i = 0, int j = 0)
        {
            return 1;
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
                p.Foo(i: 1);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext04a.ext04a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, dynamic i = null)
        {
            if (i == 1)
                return 0;
            return 1;
        }

        public static int Foo(this Parent p, dynamic i = null, int j = 0)
        {
            return 1;
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
            return p.Foo(i: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext05.ext05
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            if (i == 0)
                return 0;
            return 1;
        }

        public static int Foo(this Parent p, int i = 0, int j = 0)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext06.ext06
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }

        public static int Foo(this Parent p, int k = 0, int j = 0)
        {
            return 1;
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
                p.Foo(i: 1);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext07.ext07
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }

        public static int Foo(this Parent p, params int[] arr)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext07a.ext07a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }

        public static int Foo(this Parent p, params dynamic[] arr)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext09.ext09
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 1;
        }

        public static int Foo(this Parent p, params int[] arr)
        {
            return arr[0] - 1;
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
                p.Foo(arr: new int[]
                {
                1, 2, 3
                }

                );
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext09a.ext09a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 1;
        }

        public static int Foo(this Parent p, params dynamic[] arr)
        {
            return arr[0] - 1;
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
            return p.Foo(arr: new dynamic[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext10.ext10
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }
    }

    public static class Extend2
    {
        public static int Foo(this Parent p)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext11.ext11
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i)
        {
            return 1;
        }
    }

    public static class Extend2
    {
        public static int Foo(this Parent p, int i)
        {
            return 1;
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
                p.Foo(i: 0);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext12.ext12
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            return 1;
        }
    }

    public static class Extend2
    {
        public static int Foo(this Parent p, int i = 0)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext13.ext13
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            if (i == 1)
                return 0;
            return 1;
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
                p.Foo(p: new Parent());
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext14.ext14
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, int i = 0)
        {
            if (i == 0)
                return 0;
            return 1;
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
            return Extend.Foo(p: p);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext14a.ext14a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p, dynamic i = null)
        {
            if (i == null)
                return 0;
            return 1;
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
            return Extend.Foo(p: p);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext15.ext15
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }

        public static int Foo(this Parent p, params int[] arr)
        {
            return 1;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.extension.ext15a.ext15a
{
    // <Area> of Methods with Optional Parameters and named arguments</Area>
    // <Title>Calling extension methods properly</Title>
    // <Description>CAlling extension methods with optional parameters and named-ness</Description>
    // <Expects status=success></Expects>
    // <Code>
    public static class Extend
    {
        public static int Foo(this Parent p)
        {
            return 0;
        }

        public static int Foo(this Parent p, params dynamic[] arr)
        {
            return 1;
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
