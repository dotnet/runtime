// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate01.dlgate01
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 0);
    public class Test
    {
        private static int Boo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate01a.dlgate01a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 0);
    public class Test
    {
        public int Boo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate01b.dlgate01b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null);
    public class Test
    {
        private static int Boo(dynamic i)
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
            Foo f = Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate01c.dlgate01c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null);
    public class Test
    {
        public int Boo(dynamic i)
        {
            return 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate02.dlgate02
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        private static int Boo(int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(i: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate02a.dlgate02a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        public int Boo(int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate02b.dlgate02b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null);
    public class Test
    {
        private static int Boo(dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(i: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate02c.dlgate02c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = default(dynamic));
    public class Test
    {
        public int Boo(dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate03a.dlgate03a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, long z = 1);
    public class Test
    {
        public int Boo(int j, int k)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate04.dlgate04
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1);
    public class Test
    {
        private static int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate04a.dlgate04a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1);
    public class Test
    {
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate04b.dlgate04b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1);
    public class Test
    {
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate04c.dlgate04c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1);
    public class Test
    {
        public int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate13.dlgate13
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        private static int Boo(int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            try
            {
                f(j: 0);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgumentForDelegateInvoke, e.Message, "Foo", "j");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate14.dlgate14
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate14a.dlgate14a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        public int Boo(int j, int k, params int[] arr)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate14b.dlgate14b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(dynamic j, int k, params int[] arr)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate15.dlgate15
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return arr[0] - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate15a.dlgate15a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        public int Boo(int j, int k, params int[] arr)
        {
            return arr[0] - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate15b.dlgate15b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(dynamic j, int k, params int[] arr)
        {
            return arr[0] - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate15c.dlgate15c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1, params int[] arr);
    public class Test
    {
        public int Boo(dynamic j, int k, params int[] arr)
        {
            return arr[0] - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate16.dlgate16
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(arr: new int[]
            {
            1, 2, 3
            }

            , i: 0, z: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate16a.dlgate16a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        public int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate16b.dlgate16b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(dynamic j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(arr: new int[]
            {
            1, 2, 3
            }

            , i: 0, z: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate17.dlgate17
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            try
            {
                f(1, 2, 3, arr: new int[]
                {
                1, 2, 3
                }

                );
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "arr");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate18.dlgate18
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            try
            {
                f(1, 2, 3, j: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgumentForDelegateInvoke, e.Message, "Foo", "j");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate19.dlgate19
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return arr[0] == 3 ? 0 : 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            try
            {
                f(1, 2, 3, z: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "z");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.dlgate20.dlgate20
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            System.Console.WriteLine(j);
            System.Console.WriteLine(k);
            System.Console.WriteLine(arr);
            return arr[1] == 3 ? 0 : 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            try
            {
                f(1, 2, 3, j: 1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgumentForDelegateInvoke, e.Message, "Foo", "j");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt01.evnt01
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 1);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt01a.evnt01a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 1);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt01b.evnt01b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = null, int z = 1);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt01c.evnt01c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            dynamic z = 0;
            return eve(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt03.evnt03
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt03a.evnt03a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt03b.evnt03b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = default(dynamic), int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt04.evnt04
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt04a.evnt04a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n) => n;
            return eve(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt04b.evnt04b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = null, int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt04c.evnt04c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n) => n;
            dynamic z = 0;
            return eve(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt06.evnt06
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt06a.evnt06a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n) => n;
            return eve(z: 0, i: 4);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt06c.evnt06c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n) => n;
            dynamic z = 0;
            dynamic i = 4;
            return eve(z: z, i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt07.evnt07
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt07a.evnt07a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n) => n;
            return eve();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt19.evnt19
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt19a.evnt19a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve(z: 1, i: 2, arr: new int[]
            {
            1, 2, 3
            }

            );
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt19b.evnt19b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt19c.evnt19c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            dynamic z = 1;
            dynamic i = 2;
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            return eve(z: z, i: i, arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt20.evnt20
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt20a.evnt20a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n, arr) => n;
            return eve();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt20b.evnt20b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt21.evnt21
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt21a.evnt21a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n, arr) => arr[1];
            return eve(1, 2, 3, 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt21b.evnt21b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(dynamic j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt21c.evnt21c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n, arr) => arr[1];
            dynamic i = 3;
            return eve(1, 2, i, 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt22.evnt22
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt22a.evnt22a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n, arr) => arr.Length == 0 ? 0 : 1;
            return eve(1, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt22c.evnt22c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return k;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            eve += (m, n, arr) => arr.Length == 0 ? 0 : 1;
            dynamic i = 1;
            return eve(i, 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt23.evnt23
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt23a.evnt23a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve(z: 1, i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt23c.evnt23c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            dynamic z = 1;
            dynamic i = 2;
            return eve(z: z, i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt24.evnt24
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt24a.evnt24a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve(z: 1, i: 2, arr: null);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt24c.evnt24c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return k - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            dynamic z = 1;
            dynamic i = 2;
            dynamic arr = null;
            return eve(z: z, i: i, arr: arr);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt25.evnt25
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,19\).*CS0067</Expects>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        public int Boo(int j, int k, params int[] arr2)
        {
            return arr2[0];
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                eve += (Foo)d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt25a.evnt25a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(dynamic i = null, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(dynamic j, int k, params int[] arr2)
        {
            return arr2[0];
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            return eve(z: 0, i: 2, arr: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.evnt25c.evnt25c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates (making sure we didn't break params behaviour due to private bug)</Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 0, params int[] arr);
    public class Test
    {
        private static event Foo eve;
        private static int Boo(int j, int k, params int[] arr2)
        {
            return arr2[0];
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            eve += Boo;
            dynamic z = 0;
            dynamic i = 2;
            return eve(z: z, i: i, arr: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda01.lambda01
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple lambdas </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 0);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j) => j);
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda02.lambda02
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j) => j);
            return f(i: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda02a.lambda02a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j) => j;
            dynamic i = 0;
            return f(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda02b.lambda02b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j) => j);
            dynamic i = 0;
            return f(i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda03.lambda03
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k) => k);
            return (int)f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda03a.lambda03a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k) => k;
            dynamic z = 0;
            return (int)f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda03b.lambda03b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k) => k);
            dynamic z = 0;
            return (int)f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda04.lambda04
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k) => k);
            return (int)f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda04a.lambda04a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k) => k;
            dynamic z = 0;
            return (int)f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda04b.lambda04b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate long Foo(int i = 1, long z = 1);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k) => k);
            dynamic z = 0;
            return (int)f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda07.lambda07
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr2) => j);
            return f(arr: new int[]
            {
            1, 2, 3
            }

            , i: 0, z: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda07a.lambda07a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k, arr2) => j;
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            dynamic i = 0;
            dynamic z = 2;
            return f(arr: arr, i: i, z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda07b.lambda07b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr2) => j);
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            dynamic i = 0;
            dynamic z = 2;
            return f(arr: arr, i: i, z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda11.lambda11
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => arr[0] == 3 ? 0 : 1);
            return f(1, 2, 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda11a.lambda11a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k, arr) => arr[0] == 3 ? 0 : 1;
            dynamic a = 1;
            dynamic b = 2;
            dynamic c = 3;
            return f(a, b, c);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda11b.lambda11b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => arr[0] == 3 ? 0 : 1);
            dynamic a = 1;
            dynamic b = 2;
            dynamic c = 3;
            return f(a, b, c);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda12.lambda12
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => arr[1] == 3 ? 0 : 1);
            try
            {
                f(1, 2, 3, z: 2, j: 1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "z");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda12b.lambda12b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 5, int z = 6, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((l, k, arr) => arr[1] == 3 ? 0 : 1);
            dynamic z = 2;
            dynamic j = 1;
            dynamic c = 3;
            try
            {
                f(j, z, c, z: z, j: j);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "z");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda13.lambda13
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => k);
            return f(z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda13a.lambda13a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k, arr) => k;
            dynamic z = 0;
            return f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda13b.lambda13b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => k);
            dynamic z = 0;
            return f(z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda14.lambda14
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => k);
            return f(arr: new int[]
            {
            1, 2, 3
            }

            , z: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda14a.lambda14a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = (j, k, arr) => k;
            dynamic ary = new int[]
            {
            1, 2, 3
            }

            ;
            dynamic z = 0;
            return f(arr: ary, z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda14b.lambda14b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        private static int Boo(int j, int k, params int[] arr)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => k);
            dynamic ary = new int[]
            {
            1, 2, 3
            }

            ;
            dynamic z = 0;
            return f(arr: ary, z: z);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda15.lambda15
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => j);
            try
            {
                f(0, 2, 3, arr: new int[]
                {
                1, 2, 3
                }

                );
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "arr");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda15b.lambda15b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, ar) => j);
            dynamic arr = new int[]
            {
            1, 2, 3
            }

            ;
            try
            {
                f(0, 2, 3, arr: arr);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NamedArgumentUsedInPositional, e.Message, "arr");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda16.lambda16
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((j, k, arr) => j);
            try
            {
                f(1, 2, 3, j: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgumentForDelegateInvoke, e.Message, "Foo", "j");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.lambda16b.lambda16b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    public delegate int Foo(int i = 1, int z = 1, params int[] arr);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)((l, k, arr) => l);
            dynamic j = 2;
            try
            {
                f(1, 2, 3, j: j);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgumentForDelegateInvoke, e.Message, "Foo", "j");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr01.optattr01
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    int i);
    public class Test
    {
        private static int Boo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr01a.optattr01a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    int i);
    public class Test
    {
        public int Boo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr01b.optattr01b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    dynamic i);
    public class Test
    {
        private static int Boo(dynamic i)
        {
            return 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr01c.optattr01c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    dynamic i);
    public class Test
    {
        private static int Boo(dynamic i)
        {
            return 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr02.optattr02
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i);
    public class Test
    {
        private static int Boo(int i)
        {
            return i - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr02a.optattr02a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i);
    public class Test
    {
        public int Boo(int i)
        {
            return i - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr02b.optattr02b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i);
    public class Test
    {
        private static int Boo(dynamic i)
        {
            return i - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr02c.optattr02c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i);
    public class Test
    {
        private static int Boo(dynamic i)
        {
            return i - 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr03.optattr03
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(0)]
    int j);
    public class Test
    {
        private static int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr03a.optattr03a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(0)]
    int j);
    public class Test
    {
        public int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr03b.optattr03b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i, [Optional]
    [DefaultParameterValue(0)]
    dynamic j);
    public class Test
    {
        private static int Boo(dynamic i, dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr03c.optattr03c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i, [Optional]
    [DefaultParameterValue(0)]
    dynamic j);
    public class Test
    {
        private static int Boo(dynamic i, dynamic j)
        {
            return j ?? 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr04.optattr04
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(1)]
    int j);
    public class Test
    {
        private static int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr04a.optattr04a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(1)]
    int j);
    public class Test
    {
        public int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr04b.optattr04b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i, [Optional]
    [DefaultParameterValue(1)]
    int j);
    public class Test
    {
        private static int Boo(dynamic i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr04c.optattr04c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i, [Optional]
    [DefaultParameterValue(1)]
    dynamic j);
    public class Test
    {
        private static int Boo(dynamic i, dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(j: 0);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr05.optattr05
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(1)]
    int j);
    public class Test
    {
        private static int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(j: 0, i: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr05a.optattr05a
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(1)]
    int j);
    public class Test
    {
        public int Boo(int i, int j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            try
            {
                Foo f = d.Boo;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, e.Message, "Boo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr05b.optattr05b
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    int i, [Optional]
    [DefaultParameterValue(1)]
    dynamic j);
    public class Test
    {
        private static int Boo(int i, dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo f = Boo;
            return f(j: 0, i: 1);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.dlgate.optattr05c.optattr05c
{
    // <Area> Delegates with Optional Parameters and named arguments</Area>
    // <Title>basic delegates with named params and optionals</Title>
    // <Description>Simple delegates </Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public delegate int Foo(
    [Optional]
    [DefaultParameterValue(1)]
    dynamic i, [Optional]
    [DefaultParameterValue(1)]
    dynamic j);
    public class Test
    {
        private static int Boo(dynamic i, dynamic j)
        {
            return j;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo)Boo;
            return f(j: 0, i: 1);
        }
    }
    //</Code>
}
