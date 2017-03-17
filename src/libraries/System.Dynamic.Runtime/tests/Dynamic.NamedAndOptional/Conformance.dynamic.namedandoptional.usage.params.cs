// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms01.prms01
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms01a.prms01a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms01b.prms01b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms01c.prms01c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms02.prms02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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
            return p.Foo(x: x, y: y, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms02a.prms02a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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
            return p.Foo(x: x, y: y, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms02b.prms02b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            Parent p = new Parent();
            return p.Foo(x: x, y: y, arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms02c.prms02c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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
            return p.Foo(x: x, y: y, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms04.prms04
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms04c.prms04c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms05.prms05
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y, arr: new int[]
                {
                1, 2, 3
                }

                );
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms05c.prms05c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y, arr: new int[]
                {
                1, 2, 3
                }

                );
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms06.prms06
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params int[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y, arr: new int[]
                {
                1, 2, 3
                }

                );
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms06c.prms06c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y, params dynamic[] arr)
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
            try
            {
                p.Foo(1, 2, 3, 4, x: x, y: y, arr: new int[]
                {
                1, 2, 3
                }

                );
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms07.prms07
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 2, params int[] arr)
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
            dynamic p = new Parent();
            return p.Foo(3, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms07a.prms07a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 2, params dynamic[] arr)
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
            Parent p = new Parent();
            return p.Foo(3, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms07b.prms07b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 2, params int[] arr)
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
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            Parent p = new Parent();
            return p.Foo(3, arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms07c.prms07c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x, int y = 2, params dynamic[] arr)
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
            dynamic p = new Parent();
            return p.Foo(3, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms08.prms08
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2)
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
            return p.Foo(arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms08a.prms08a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2)
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
            return p.Foo(arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms08b.prms08b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2)
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
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            Parent p = new Parent();
            return p.Foo(arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms08c.prms08c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2)
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
            return p.Foo(arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms09.prms09
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2)
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
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms09a.prms09a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms09b.prms09b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2)
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
            dynamic i = 1;
            Parent p = new Parent();
            return p.Foo(i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms09c.prms09c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>calling NPs with params in signatures</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2)
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
            return p.Foo(1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms10.prms10
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2 && arr.Length == 1 && arr[0] == 3)
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
            return p.Foo(arr: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms10a.prms10a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2 && arr.Length == 1 && arr[0] == 3)
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
            return p.Foo(arr: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms10b.prms10b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            if (x == 1 && y == 2 && arr.Length == 1 && arr[0] == 3)
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
            dynamic arr = 3;
            Parent p = new Parent();
            return p.Foo(arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms10c.prms10c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            if (x == 1 && y == 2 && arr.Length == 1 && arr[0] == 3)
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
            dynamic[] arr = new dynamic[]
            {
            3
            }

            ;
            return p.Foo(arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms12.prms12
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            return arr[0] == 3 ? 0 : 1;
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
            return p.Foo(arr: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms12a.prms12a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            return arr[0] == 3 ? 0 : 1;
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
            return p.Foo(arr: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms12b.prms12b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params int[] arr)
        {
            return arr[0] == 3 ? 0 : 1;
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
            dynamic arr = 3;
            Parent p = new Parent();
            return p.Foo(arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.prms.prms12c.prms12c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use named arguments to params array</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int x = 1, int y = 2, params dynamic[] arr)
        {
            return arr[0] == 3 ? 0 : 1;
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
            dynamic[] arr = new dynamic[]
            {
            3
            }

            ;
            return p.Foo(arr: arr);
        }
    }
    //</Code>
}
