// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.negative.neg001.neg001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: The operator is *=, /=, %=, &=, |=, ^=, <<=, >>=
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E = x => x;
        public static int Foo(int i)
        {
            return i;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            int result = 0;
            try
            {
                c.E *= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "*=", "Dele", "Dele"))
                    result += 1;
            }

            try
            {
                c.E /= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "/=", "Dele", "Dele"))
                    result += 2;
            }

            try
            {
                c.E %= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "Dele", "Dele"))
                    result += 4;
            }

            try
            {
                c.E &= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "&=", "Dele", "Dele"))
                    result += 8;
            }

            try
            {
                c.E |= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "|=", "Dele", "Dele"))
                    result += 16;
            }

            try
            {
                c.E ^= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "^=", "Dele", "Dele"))
                    result += 32;
            }

            try
            {
                c.E <<= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "Dele", "Dele"))
                    result += 64;
            }

            try
            {
                c.E >>= new Dele(C.Foo);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, ">>=", "Dele", "Dele"))
                    result += 128;
            }

            if (result != 255)
            {
                System.Console.WriteLine("result = {0}", result);
                return 1;
            }

            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.negative.neg002.neg002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is dynamic expression with runtime non-delegate type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo(int i)
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
            dynamic c = new C();
            try
            {
                c.E += c;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // incorrect error message
                //  resolution is 'By design' so this error message should be use again
                // new error message
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "C", "Dele"))
                    return 0;
            }

            return 1;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.negative.neg003.neg003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is non-matched delegate
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo(int i)
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
            dynamic c = new C();
            int result = 0;
            try
            {
                c.E += (Func<int, int>)(x => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int>", "Dele"))
                    result += 1;
            }

            try
            {
                c.E -= (Func<int, int>)(x => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int>", "Dele"))
                    result += 2;
            }

            if (result != 3)
            {
                System.Console.WriteLine("Result = {0}", result);
                return 1;
            }

            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.negative.neg004.neg004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is compile time known type and it's non valid type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo(int i)
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
            dynamic c = new C();
            int result = 0;
            try
            {
                c.E += new C();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "C", "Dele"))
                    result++;
            }

            try
            {
                c.E += 1;
            } // : not type info for c.E at runtime as it's null
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "int", "Dele"))
                    result++;
                ;
            }

            try
            {
                c.E -= new C();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "C", "Dele"))
                    result++;
                ;
            }

            try
            {
                c.E -= 1; // : not type info for c.E at runtime as it's null
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "int", "Dele"))
                    result++;
                ;
            }

            return result;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.negative.neg005.neg005
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is non-matched delegate and lhs is null
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(17,23\).*CS0067</Expects>
    using System;

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            int result = 0;
            try
            {
                c.E += (Func<int, int>)(x => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int>", "Dele"))
                    result += 1;
            }

            try
            {
                c.E += (Func<int, int, int>)((x, y) => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int,int>", "Dele"))
                    result += 2;
            }

            try
            {
                //  - no error for -= (by design :()
                // it seems that it is fixed now.
                c.E -= (Func<int, int>)(x => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int>", "Dele"))
                    result += 4;
            }

            try
            {
                //  - no error for -= (by design :()
                c.E -= (Func<int, int, int>)((x, y) => x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "System.Func<int,int,int>", "Dele"))
                    result += 8;
            }

            return (result == 15) ? 0 : 1;
        }
    }
    // </Code>
}
