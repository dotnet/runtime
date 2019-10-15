// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if CAP_TypeOfPointer

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.arrayaccess01.arrayaccess01
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.arrayaccess01.arrayaccess01;
    // <Area> dynamic in unsafe code </Area>
    // <Title>pointer operator</Title>
    // <Description>
    // member access
    // </Description>
    // <RelatedBug>564384</RelatedBug>
    //<Expects Status=success></Expects>
    // <Code>


    [TestClass]
    public unsafe class Test
    {
        [Test]
        [Priority(Priority.Priority0)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod(null)); }
        public static int MainMethod(string[] args)
        {
            int* ptr = stackalloc int[10];
            for (int i = 0; i < 10; i++)
            {
                *(ptr + i) = i;
            }

            dynamic d = 5;

            int x = ptr[d];

            if (x != 5) return 1;
            return 0;

        }
    }


    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.arrayaccess02.arrayaccess02
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.arrayaccess02.arrayaccess02;
    // <Area> dynamic with pointer indexer </Area>
    // <Title>pointer operator</Title>
    // <Description>
    // member access
    // </Description>
    // <RelatedBug></RelatedBug>
    //<Expects Status=success></Expects>
    // <Code>


    [TestClass]
    public unsafe class Test
    {
        [Test]
        [Priority(Priority.Priority0)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod(null)); }
        public static int MainMethod(string[] args)
        {
            int* ptr = stackalloc int[10];
            for (int i = 0; i < 10; i++)
            {
                *(ptr + i) = i;
            }

            int test = 0, success = 0;
            dynamic d;
            int x;

            test++;
            d = (uint)5;
            x = ptr[d];
            if (x == 5) success++;

            test++;
            d = (ulong)5;
            x = ptr[d];
            if (x == 5) success++;

            test++;
            d = (long)5;
            x = ptr[d];
            if (x == 5) success++;

            return test == success ? 0 : 1;

        }
    }


    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.sizeof01.sizeof01
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.sizeof01.sizeof01;
    // <Area> dynamic in unsafe code </Area>
    // <Title>pointer operator</Title>
    // <Description>
    // sizeof operator
    // </Description>
    // <RelatedBug></RelatedBug>
    //<Expects Status=success></Expects>
    // <Code>

    [TestClass]
    public unsafe class Test
    {
        [Test]
        [Priority(Priority.Priority1)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod(null)); }
        public static int MainMethod(string[] args)
        {
            dynamic d = sizeof(int);
            return 0;
        }
    }

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.stackalloc01.stackalloc01
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.stackalloc01.stackalloc01;
    // <Area> dynamic in unsafe code </Area>
    // <Title>pointer operator</Title>
    // <Description>
    // stackalloc
    // </Description>
    // <RelatedBug></RelatedBug>
    //<Expects Status=success></Expects>
    // <Code>

    [TestClass]
    public unsafe class Test
    {
        [Test]
        [Priority(Priority.Priority1)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod(null)); }
        public static int MainMethod(string[] args)
        {
            dynamic d = 10;
            int* ptr = stackalloc int[d];

            return 0;
        }
    }

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.pointegeregerertype01.pointegeregerertype01
{
    using ManagedTests.DynamicCSharp.Test;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.PointegeregererOperator.pointegeregerertype01.pointegeregerertype01;
    // <Area> dynamic in unsafe code </Area>
    // <Title>Regression</Title>
    // <Description>
    // VerificationException thrown when dynamically dispatching a method call with out/ref arguments which are pointer types
    // </Description>
    // <RelatedBug></RelatedBug>
    // <Expects Status=success></Expects>
    // <Code>

    using System;
    using System.Security;
    [TestClass]
    public class TestClass
    {
        public unsafe void Method(out int* arg)
        {
            arg = (int*)5;
        }
    }

    struct Driver
    {
        [Test]
        [Priority(Priority.Priority2)]
        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
        public static unsafe int MainMethod()
        {
            int* ptr = null;
            dynamic tc = new TestClass();
            try
            {
                tc.Method(out ptr);
            }
            catch (VerificationException e)
            {
                return 0; //this was won't fix
            }
            return 1;
        }
    }


    // </Code>
}

#endif
