// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion001.conversion001
{
    // <Title> Compound operator in conversion.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Base1
    {
        public int field;
    }

    public class Base2
    {
        public int field;
    }

    public class TestClass
    {
        public int field;
        public static implicit operator TestClass(Base2 p1)
        {
            return new TestClass()
            {
                field = p1.field
            }

            ;
        }

        public static Base2 operator |(TestClass p1, Base1 p2)
        {
            return new Base2()
            {
                field = (p1.field | p2.field)
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            TestClass l = new TestClass()
            {
                field = 8
            }

            ;
            Base1 r = new Base1()
            {
                field = 2
            }

            ;
            dynamic d0 = l;
            d0 |= r;
            dynamic d1 = l;
            dynamic d2 = r;
            d1 |= d2;
            l |= d2;
            if (d0.field == 10 && d1.field == 10 && l.field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion002.conversion002
{
    // <Title> Compound operator in conversion.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Base1
    {
        public int field;
    }

    public class Base2
    {
        public int field;
    }

    public class TestClass
    {
        public int field;
        public static explicit operator TestClass(Base2 p1)
        {
            return new TestClass()
            {
                field = p1.field
            }

            ;
        }

        public static Base2 operator |(TestClass p1, Base1 p2)
        {
            return new Base2()
            {
                field = (p1.field | p2.field)
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            TestClass l = new TestClass()
            {
                field = 8
            }

            ;
            Base1 r = new Base1()
            {
                field = 2
            }

            ;
            int flag = 0;
            dynamic d0 = l;
            try
            {
                d0 |= r;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestClass"))
                {
                    flag++;
                }
            }

            dynamic d1 = l;
            dynamic d2 = r;
            try
            {
                d1 |= d2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestClass"))
                {
                    flag++;
                }
            }

            try
            {
                l |= d2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestClass"))
                {
                    flag++;
                }
            }

            if (flag == 0)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion003.conversion003
{
    // <Title> Compound operator in conversion.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int field;
        public static implicit operator Test(string p1)
        {
            return new Test()
            {
                field = 10
            }

            ;
        }

        public static Test operator +(Test p1, Test p2)
        {
            return new Test()
            {
                field = (p1.field + p2.field)
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            dynamic d0 = new Test()
            {
                field = 2
            }

            ;
            d0 += "";
            dynamic d1 = new Test()
            {
                field = 2
            }

            ;
            d1 += "A";
            Test d2 = new Test()
            {
                field = 2
            }

            ;
            dynamic t2 = "";
            d2 += t2;
            dynamic d3 = new Test()
            {
                field = 2
            }

            ;
            dynamic t3 = string.Empty;
            d3 += t3;
            if (d0.field == 12 && d1.field == 12 && d2.field == 12 && d3.field == 12)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion005.conversion005
{
    // <Title> Compound operator in conversion.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Base1
    {
        public int field;
    }

    public struct Base2
    {
        public int field;
    }

    public struct TestStruct
    {
        public int field;
        public static explicit operator TestStruct(Base2 p1)
        {
            return new TestStruct()
            {
                field = p1.field
            }

            ;
        }

        public static Base2 operator *(TestStruct p1, Base1 p2)
        {
            return new Base2()
            {
                field = (p1.field * 2)
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            TestStruct l = new TestStruct()
            {
                field = 8
            }

            ;
            Base1 r = new Base1()
            {
                field = 2
            }

            ;
            int flag = 0;
            dynamic d0 = l;
            try
            {
                d0 *= r;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestStruct"))
                {
                    flag++;
                }
            }

            dynamic d1 = l;
            try
            {
                d1 *= null;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestStruct"))
                {
                    flag++;
                }
            }

            dynamic d2 = l;
            dynamic d3 = r;
            try
            {
                d2 *= d3;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestStruct"))
                {
                    flag++;
                }
            }

            try
            {
                l *= d3;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Base2", "TestStruct"))
                {
                    flag++;
                }
            }

            if (flag == 0)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion006.conversion006
{
    // <Title> Compound operator in conversion(negative).</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Base1
    {
        public int field;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test l = new Test();
            Base1 r = new Base1()
            {
                field = 2
            }

            ;
            try
            {
                dynamic d0 = l;
                d0 *= r;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "*=", "Test", "Base1"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.conversion.conversion007.conversion007
{
    // <Title> Compound operator in conversion.</Title>
    // <Description>
    // Compound operators (d += 10) is Expanding to (d = d + 10), it turns out this does match
    // the semantics of the equivalent static production.
    // In the static context, (t += 10) will result of type Test, but in dynamic, the result is type int.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public class Test
    {
        private int _f1 = 10;
        public Test()
        {
        }

        public Test(int p1)
        {
            _f1 = p1;
        }

        public static implicit operator Test(int p1)
        {
            return new Test(p1);
        }

        public static int operator +(Test t, int p1)
        {
            return t._f1 + p1;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            if ((((d += 10).GetType()) == typeof(int)) && (d.GetType() == typeof(int)))
            {
                return 0;
            }

            return 1;
        }
    }
    //</Code>
}
