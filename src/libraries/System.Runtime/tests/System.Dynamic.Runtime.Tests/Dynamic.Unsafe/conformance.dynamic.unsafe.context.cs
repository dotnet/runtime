// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.class01.class01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // class
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public static int* p;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.codeblock01.codeblock01
{
    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            unsafe
            {
                dynamic d = 1;
                d = new Test();
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.freach01.freach01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe context </Title>
    // <Description>
    // foreach
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public static int* p;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            C[] arrayC = new C[]
            {
            new C(), new C(), new C()}

            ;
            foreach (dynamic d in arrayC) // C is an unsafe type
            {
                if (d.GetType() != typeof(C))
                {
                    return 1;
                }
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.freach02.freach02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe context </Title>
    // <Description>
    // foreach
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public static int* p;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic arrayC = new C[]
            {
            new C(), new C(), new C()}

            ;
            foreach (C c in arrayC) // C is an unsafe type
            {
                if (c.GetType() != typeof(C))
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.freach03.freach03
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe context </Title>
    // <Description>
    // foreach
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    // pointer arrays are not supported
    //public unsafe class C
    //{
    //public static int* p;
    //}
    //[TestClass]public class Test
    //{
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod(null));} public static unsafe int MainMethod(string[] args)
    //{
    //int a = 1, b = 2, c = 3;
    //dynamic arrayp = new int*[] { &a, &b, &c };
    //try
    //{
    //foreach (dynamic d in arrayp)
    //{
    //}
    //}
    //catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
    //{
    //if (ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message))
    //return 0;
    //}
    //return 1;
    //}
    //}
    //// </Code>
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.strct01.strct01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type</Title>
    // <Description>
    // struct
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe struct S
    {
        public static int* p;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new S();
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.conformance.dynamic.unsfe.context.while01.while01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe context </Title>
    // <Description>
    // foreach
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public static int* p;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            unsafe
            {
                int index = 5;
                do
                {
                    dynamic d = new C();
                    int* p = &index;
                    *p = *p - 1;
                }
                while (index > 0);
            }

            return 0;
        }
    }
    // </Code>
}
