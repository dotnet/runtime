// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.bolliteral001.bolliteral001
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(bool b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(false); //this should fit string
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.bolliteral002.bolliteral002
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(bool b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(true); //this should fit string
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral001.chrliteral001
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('a'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral002.chrliteral002
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\''); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral003.chrliteral003
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\"'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral004.chrliteral004
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\\'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral005.chrliteral005
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\0'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral006.chrliteral006
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\a'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral007.chrliteral007
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\b'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral008.chrliteral008
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\f'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral009.chrliteral009
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\n'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral010.chrliteral010
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method('\r'); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.chrliteral011.chrliteral011
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method((char)0x0023); //this should fit char
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral001.integeregeregerliteral001
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345u); //this should fit uint
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral002.integeregeregerliteral002
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(9223372036854775808U); //this should fit ulong
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral003.integeregeregerliteral003
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345u); //this should fit uint
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral004.integeregeregerliteral004
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(26,18\).*CS0078</Expects>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(9223372036854775808l); //this should fit ulong
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral005.integeregeregerliteral005
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(58,18\).*CS0078</Expects>
    //<Expects Status=warning>\(63,18\).*CS0078</Expects>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            Test.Status = 1;
            d.Method(9223372036854775808ul); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1Ul); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1uL); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1UL); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1LU); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1Lu); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1lU); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            Test.Status = 1;
            d.Method(1lu); //this should fit ulong
            if (Test.Status != 2)
                return 1;
            return 0;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral006.integeregeregerliteral006
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        public void Method(long b)
        {
            Test.Status = 3;
        }

        public void Method(ulong b)
        {
            Test.Status = 4;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(2147483647);
            if (Test.Status != 1)
                return 1;
            d.Method(4294967295);
            if (Test.Status != 2)
                return 1;
            d.Method(9223372036854775807);
            if (Test.Status != 3)
                return 1;
            d.Method(18446744073709551615);
            if (Test.Status != 4)
                return 1;
            return 0;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral007.integeregeregerliteral007
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(-2147483648);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral008.integeregeregerliteral008
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(-9223372036854775808);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.integeregeregerliteral009.integeregeregerliteral009
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(dynamic b)
        {
            Test.Status = 1;
        }

        public void Method(long b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(9223372036854775808U); //this should fit dynamic
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral001.nullliteral001
{
    public class Test
    {
        public static int Status;
        public void Method(float? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(null);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral002.nullliteral002
{
    // <Title> Tests overload resolution when a null literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(Base b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(null);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral003.nullliteral003
{
    public class Test
    {
        public static int Status;
        public void Method(float? b)
        {
            Test.Status = 1;
        }

        public void Method(string S)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method((float?)null);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral004.nullliteral004
{
    public class Test
    {
        public void Method(float? b)
        {
        }

        public void Method(string S)
        {
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
                d.Method(null);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Test.Method(float?)", "Test.Method(string)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral005.nullliteral005
{
    public class Test
    {
        public static int Status;
        public void Method(float? b, int x)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            int x = 3;
            d.Method(null, 3);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral006.nullliteral006
{
    // <Title> Tests overload resolution when a null literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(Base b, int x)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            int x = 3;
            d.Method(null, x);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.nullliteral007.nullliteral007
{
    public class Test
    {
        public void Method(float? b, int x)
        {
        }

        public void Method(string S, int x)
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            int x = 3;
            try
            {
                d.Method(null, x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Test.Method(float?, int)", "Test.Method(string, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral001.realliteral001
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345f); //this should fit float
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral002.realliteral002
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345d); //this should fit double
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral003.realliteral003
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(decimal b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345m); //this should fit decimal
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral004.realliteral004
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status = 0;
        public void Method(decimal b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(345);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral005.realliteral005
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(1e03f); //this should fit float
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral006.realliteral006
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(1e03d); //this should fit double
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.realliteral007.realliteral007
{
    // <Title> Tests overload resolution when a numeral literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(decimal b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(1e03m); //this should fit decimal
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.strliteral001.strliteral001
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(string b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method("foo"); //this should fit string
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.strliteral002.strliteral002
{
    // <Title> Tests overload resolution when a char literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(string b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(@"c:\foo"); //this should fit string
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral001.zeroliteral001
{
    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral002.zeroliteral002
{
    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral003.zeroliteral003
{
    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral004.zeroliteral004
{
    public class Test
    {
        public static int Status;
        public void Method(decimal b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral005.zeroliteral005
{
    public class Test
    {
        public static int Status;
        public void Method(double b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral006.zeroliteral006
{
    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral007.zeroliteral007
{
    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral008.zeroliteral008
{
    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral009.zeroliteral009
{
    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral010.zeroliteral010
{
    public class Test
    {
        public static int Status;
        public void Method(int? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral011.zeroliteral011
{
    public class Test
    {
        public static int Status;
        public void Method(float? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral012.zeroliteral012
{
    public class Test
    {
        public static int Status = -1;
        public void Method(decimal? b)
        {
            Test.Status = 1;
        }

        public void Method1(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            bool ret = Test.Status == 1;
            d.Method1(0);
            ret &= Test.Status == 2;
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral013.zeroliteral013
{
    public class Test
    {
        public static int Status;
        public void Method(double? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral014.zeroliteral014
{
    public class Test
    {
        public static int Status;
        public void Method(byte? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral015.zeroliteral015
{
    public class Test
    {
        public static int Status;
        public void Method(ushort? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral016.zeroliteral016
{
    public class Test
    {
        public static int Status;
        public void Method(ushort? b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral017.zeroliteral017
{
    public class Test
    {
        public static int Status;
        public void Method(char b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method((char)0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral018.zeroliteral018
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public enum Enumeration
    {
        One,
        Two
    }

    public class Test
    {
        public static int Status;
        public void Method(Enumeration b)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteral019.zeroliteral019
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public enum Enumeration
    {
        One,
        Two
    }

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(Enumeration b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            //This should be an ambiguous error
            try
            {
                d.Method(0);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Test.Method(short)", "Test.Method(Enumeration)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution001.zeroliteralresolution001
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(short b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution002.zeroliteralresolution002
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(int b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution003.zeroliteralresolution003
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(long b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution004.zeroliteralresolution004
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(byte b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution005.zeroliteralresolution005
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(ushort b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution006.zeroliteralresolution006
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution007.zeroliteralresolution007
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution008.zeroliteralresolution008
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution009.zeroliteralresolution009
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution010.zeroliteralresolution010
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(sbyte b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution011.zeroliteralresolution011
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(int b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution012.zeroliteralresolution012
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(long b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution013.zeroliteralresolution013
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(byte b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution014.zeroliteralresolution014
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(ushort b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution015.zeroliteralresolution015
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution016.zeroliteralresolution016
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution017.zeroliteralresolution017
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution018.zeroliteralresolution018
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution019.zeroliteralresolution019
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(short b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution020.zeroliteralresolution020
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(long b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution021.zeroliteralresolution021
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(byte b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution022.zeroliteralresolution022
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(ushort b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution023.zeroliteralresolution023
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution024.zeroliteralresolution024
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution025.zeroliteralresolution025
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution026.zeroliteralresolution026
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution027.zeroliteralresolution027
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(int b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution028.zeroliteralresolution028
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(byte b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution029.zeroliteralresolution029
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(ushort b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution030.zeroliteralresolution030
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution031.zeroliteralresolution031
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution032.zeroliteralresolution032
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution033.zeroliteralresolution033
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution034.zeroliteralresolution034
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(long b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution035.zeroliteralresolution035
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(ushort b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution036.zeroliteralresolution036
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution037.zeroliteralresolution037
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution038.zeroliteralresolution038
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution039.zeroliteralresolution039
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution040.zeroliteralresolution040
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(byte b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution041.zeroliteralresolution041
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        public void Method(uint b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution042.zeroliteralresolution042
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution043.zeroliteralresolution043
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution044.zeroliteralresolution044
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution045.zeroliteralresolution045
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ushort b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution046.zeroliteralresolution046
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(ulong b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution047.zeroliteralresolution047
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution048.zeroliteralresolution048
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution049.zeroliteralresolution049
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(uint b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution050.zeroliteralresolution050
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ulong b)
        {
            Test.Status = 1;
        }

        public void Method(float b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution051.zeroliteralresolution051
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ulong b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution052.zeroliteralresolution052
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(ulong b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution053.zeroliteralresolution053
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(double b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.literals.zeroliteralresolution054.zeroliteralresolution054
{
    // <Title> Tests overload resolution when a zero literal is passed to the binder</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Status;
        public void Method(float b)
        {
            Test.Status = 1;
        }

        public void Method(decimal b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.Method(0f);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    } //</Code>
}
