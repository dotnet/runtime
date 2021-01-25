// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum001.enum001
{
    // <Area>Enum -- binary operator -- user-defined conversion</Area>
    // <Title>Enum operators with user-defined implicit conversion</Title>
    // <Description>
    // The overload resolution select predefined enum binary operators only when one operand is enum type.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3
    }

    public class A
    {
        public static implicit operator E(A x)
        {
            return E.EM2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            dynamic dobj = new A();
            dynamic dobj2 = new A();
            dynamic r = dobj - E.EM1;
            if ((r.GetType() != typeof(int)) || ((int)r != 1))
                result++;
            r = E.EM2 & dobj;
            if ((r.GetType() != typeof(E)) || ((E)r != E.EM2))
                result++;
            r = dobj | E.EM1;
            if ((r.GetType() != typeof(E)) || ((E)r != E.EM3))
                result++;
            r = E.EM1 ^ dobj;
            if ((r.GetType() != typeof(E)) || ((E)r != E.EM3))
                result++;
            r = dobj == E.EM1;
            if ((r.GetType() != typeof(bool)) || ((bool)r != false))
                result++;
            r = E.EM3 != dobj;
            if ((r.GetType() != typeof(bool)) || ((bool)r != true))
                result++;
            r = dobj > E.EM1;
            if ((r.GetType() != typeof(bool)) || ((bool)r != true))
                result++;
            r = E.EM2 >= dobj;
            if ((r.GetType() != typeof(bool)) || ((bool)r != true))
                result++;
            r = dobj < E.EM1;
            if ((r.GetType() != typeof(bool)) || ((bool)r != false))
                result++;
            r = E.EM1 <= dobj;
            if ((r.GetType() != typeof(bool)) || ((bool)r != true))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum001b.enum001b
{
    // <Area>Enum -- binary operator -- user-defined conversion</Area>
    // <Title>Enum operators with user-defined implicit conversion</Title>
    // <Description>
    // The overload resolution select predefined enum binary operators only when one operand is enum type.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>
    public enum E
    {
        EM0,
        EM1
    }

    public class A
    {
        public static implicit operator E(A x)
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
            int result = 0;
            int flag = 1;
            dynamic dobj = new A();
            dynamic dobj2 = new A();
            dynamic dr;
            flag = 1;
            try
            {
                dr = dobj + 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "A", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = dobj + E.EM0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "A", "E"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = 0 + dobj;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "int", "A"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = dobj - 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "A", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = dobj - dobj2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "A", "A"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = dobj & dobj2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "A", "A"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dr = dobj > dobj2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "A", "A"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum002.enum002
{
    // <Area>Enum -- binary operator</Area>
    // <Title>Enum binary operators with one operand is null constant</Title>
    // <Description>
    // It should get ambiguity error because of both "E? -(E?, E?)" and "E? -(E?, int?)" are candidate
    // and neither is better than the other.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            dynamic dobj = new E();
            dynamic r = dobj - null;
            if (r != null)
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum003.enum003
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Overload resolution choose enum binary operator only while one operand is enum type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        Zero,
        One,
        Two
    }

    public class C
    {
        public static implicit operator E(C s)
        {
            System.Console.WriteLine("imp C ==> E");
            return E.One;
        }

        public static implicit operator int (C s)
        {
            return 3;
        }
    }

    public struct S
    {
        public static implicit operator E(S s)
        {
            System.Console.WriteLine("imp S ==> E");
            return E.One;
        }

        public static implicit operator int (S s)
        {
            return 3;
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            dynamic s = new S();
            var r1 = s + s;
            if ((r1.GetType() != typeof(int)) || (r1 != 6))
                result++;
            dynamic c = new C();
            var r2 = c + c;
            if ((r2.GetType() != typeof(int)) || (r2 != 6))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004.enum004
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E(ConvE s)
        {
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int (ConvInt s)
        {
            return 2;
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
            int result = 0;
            ConvInt toint = new ConvInt();
            ConvE toe = new ConvE();
            dynamic dobj;
            dobj = E.EM1;
            var r1 = dobj + toint;
            if ((r1.GetType() != typeof(E)) || (r1 != E.EM3))
                result++;
            dobj = E.EM2;
            var r2 = toint + dobj;
            if ((r2.GetType() != typeof(E)) || (r2 != E.EM4))
                result++;
            dobj = E.EM3;
            var r3 = dobj - toe;
            if ((r3.GetType() != typeof(int)) || (r3 != 2))
                result++;
            dobj = E.EM3;
            var r4 = dobj - toint;
            if ((r4.GetType() != typeof(E)) || (r4 != E.EM1))
                result++;
            dobj = E.EM1;

            var r5 = toint - dobj;
            if ((r5.GetType() != typeof(E)) || (r5 != E.EM1))
                result++;
            dobj = E.EM1;
            var r10 = dobj == toe;
            if ((r10.GetType() != typeof(bool)) || (r10 != true))
                result++;
            dobj = E.EM1;
            var r11 = toe != dobj;
            if ((r11.GetType() != typeof(bool)) || (r11 != false))
                result++;
            dobj = E.EM3;
            var r12 = dobj > toe;
            if ((r12.GetType() != typeof(bool)) || (r12 != true))
                result++;
            dobj = E.EM0;
            var r13 = toe < dobj;
            if ((r13.GetType() != typeof(bool)) || (r13 != false))
                result++;
            dobj = E.EM2;
            var r14 = dobj >= toe;
            if ((r14.GetType() != typeof(bool)) || (r14 != true))
                result++;
            dobj = E.EM1;
            var r15 = toe <= dobj;
            if ((r15.GetType() != typeof(bool)) || (r15 != true))
                result++;
            dobj = E.EM1;
            var r20 = dobj & toe;
            if ((r20.GetType() != typeof(E)) || (r20 != E.EM1))
                result++;
            dobj = E.EM2;
            var r21 = toe | dobj;
            if ((r21.GetType() != typeof(E)) || (r21 != E.EM3))
                result++;
            dobj = E.EM3;
            var r22 = dobj ^ toe;
            if ((r22.GetType() != typeof(E)) || (r22 != E.EM2))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004b.enum004b
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E(ConvE s)
        {
            System.Console.WriteLine("imp ConvE ==> E");
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int (ConvInt s)
        {
            System.Console.WriteLine("imp ConvInt ==> int");
            return 2;
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
            int result = 0;
            int flag = 1;
            ConvInt toint = new ConvInt();
            ConvE toe = new ConvE();
            dynamic dobj;
            dynamic r;
            flag = 1;
            dobj = E.EM1;
            try
            {
                r = dobj + toe; // no +(E, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "E", "ConvE"))
                {
                    flag = 0;
                }
            }

            result += flag;
            //r = toint - E.EM1;          // no -(U, E)
            flag = 1;
            dobj = E.EM1;
            try
            {
                r = toint == dobj; // no ==(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "ConvInt", "E"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            dobj = E.EM1;
            try
            {
                r = dobj > toint; // no >(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "E", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            dobj = E.EM1;
            try
            {
                r = dobj & toint; // no &(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "E", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            dobj = E.EM3;
            try
            {
                r = toint ^ dobj; // no ^(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "ConvInt", "E"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004c.enum004c
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E(ConvE s)
        {
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int (ConvInt s)
        {
            return 2;
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
            int result = 0;
            dynamic toint = new ConvInt();
            dynamic toe = new ConvE();
            E? e;
            e = E.EM1;
            var r1 = e + toint;
            if ((r1.GetType() != typeof(E)) || (r1 != E.EM3))
                result++;
            e = E.EM2;
            var r2 = toint + e;
            if ((r2.GetType() != typeof(E)) || (r2 != E.EM4))
                result++;
            e = E.EM3;
            var r3 = e - toe;
            if ((r3.GetType() != typeof(int)) || (r3 != 2))
                result++;
            e = E.EM3;
            var r4 = e - toint;
            if ((r4.GetType() != typeof(E)) || (r4 != E.EM1))
                result++;
            e = E.EM1;

            var r5 = toint - e;
            if ((r5.GetType() != typeof(E)) || (r5 != E.EM1))
                result++;
            e = E.EM1;
            var r10 = e == toe;
            if ((r10.GetType() != typeof(bool)) || (r10 != true))
                result++;
            e = E.EM1;
            var r11 = toe != e;
            if ((r11.GetType() != typeof(bool)) || (r11 != false))
                result++;
            e = E.EM3;
            var r12 = e > toe;
            if ((r12.GetType() != typeof(bool)) || (r12 != true))
                result++;
            e = E.EM0;
            var r13 = toe < e;
            if ((r13.GetType() != typeof(bool)) || (r13 != false))
                result++;
            e = E.EM2;
            var r14 = e >= toe;
            if ((r14.GetType() != typeof(bool)) || (r14 != true))
                result++;
            e = E.EM1;
            var r15 = toe <= e;
            if ((r15.GetType() != typeof(bool)) || (r15 != true))
                result++;
            e = E.EM1;
            var r20 = e & toe;
            if ((r20.GetType() != typeof(E)) || (r20 != E.EM1))
                result++;
            e = E.EM2;
            var r21 = toe | e;
            if ((r21.GetType() != typeof(E)) || (r21 != E.EM3))
                result++;
            e = E.EM3;
            var r22 = e ^ toe;
            if ((r22.GetType() != typeof(E)) || (r22 != E.EM2))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004d.enum004d
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E(ConvE s)
        {
            System.Console.WriteLine("imp ConvE ==> E");
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int (ConvInt s)
        {
            System.Console.WriteLine("imp ConvInt ==> int");
            return 2;
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
            int result = 0;
            int flag = 1;
            dynamic toint = new ConvInt();
            dynamic toe = new ConvE();
            E? e;
            dynamic r;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e + toe; // no +(E, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "E?", "ConvE"))
                {
                    flag = 0;
                }
            }

            result += flag;

            //r = toint - E.EM1;          // no -(U, E)
            flag = 1;
            e = E.EM1;
            try
            {
                r = toint == e; // no ==(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "ConvInt", "E?"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e > toint; // no >(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "E?", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e & toint; // no &(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "E?", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM3;
            try
            {
                r = toint ^ e; // no ^(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "ConvInt", "E?"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004e.enum004e
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E? (ConvE s)
        {
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int? (ConvInt s)
        {
            return 2;
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
            int result = 0;
            dynamic toint = new ConvInt();
            dynamic toe = new ConvE();
            E? e;
            e = E.EM1;
            var r1 = e + toint;
            if ((r1.GetType() != typeof(E)) || (r1 != E.EM3))
                result++;
            e = E.EM2;
            var r2 = toint + e;
            if ((r2.GetType() != typeof(E)) || (r2 != E.EM4))
                result++;
            e = E.EM3;
            var r3 = e - toe;
            if ((r3.GetType() != typeof(int)) || (r3 != 2))
                result++;
            e = E.EM3;
            var r4 = e - toint;
            if ((r4.GetType() != typeof(E)) || (r4 != E.EM1))
                result++;
            e = E.EM1;

            var r5 = toint - e;
            if ((r5.GetType() != typeof(E)) || (r5 != E.EM1))
                result++;
            e = E.EM1;
            var r10 = e == toe;
            if ((r10.GetType() != typeof(bool)) || (r10 != true))
                result++;
            e = E.EM1;
            var r11 = toe != e;
            if ((r11.GetType() != typeof(bool)) || (r11 != false))
                result++;
            e = E.EM3;
            var r12 = e > toe;
            if ((r12.GetType() != typeof(bool)) || (r12 != true))
                result++;
            e = E.EM0;
            var r13 = toe < e;
            if ((r13.GetType() != typeof(bool)) || (r13 != false))
                result++;
            e = E.EM2;
            var r14 = e >= toe;
            if ((r14.GetType() != typeof(bool)) || (r14 != true))
                result++;
            e = E.EM1;
            var r15 = toe <= e;
            if ((r15.GetType() != typeof(bool)) || (r15 != true))
                result++;
            e = E.EM1;
            var r20 = e & toe;
            if ((r20.GetType() != typeof(E)) || (r20 != E.EM1))
                result++;
            e = E.EM2;
            var r21 = toe | e;
            if ((r21.GetType() != typeof(E)) || (r21 != E.EM3))
                result++;
            e = E.EM3;
            var r22 = e ^ toe;
            if ((r22.GetType() != typeof(E)) || (r22 != E.EM2))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum004f.enum004f
{
    // <Area>Enum -- binary operators -- user-defined conversion</Area>
    // <Title>Enum binary operators with one operand is user-defined implicit conversion to enum type</Title>
    // <Description>
    // Overload resolution choose enum binary operator only while one operand is enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class ConvE
    {
        public static implicit operator E? (ConvE s)
        {
            System.Console.WriteLine("imp ConvE ==> E?");
            return E.EM1;
        }
    }

    public struct ConvInt
    {
        public static implicit operator int? (ConvInt s)
        {
            System.Console.WriteLine("imp ConvInt ==> int?");
            return 2;
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
            int result = 0;
            int flag = 1;
            dynamic toint = new ConvInt();
            dynamic toe = new ConvE();
            E? e;
            dynamic r;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e + toe; // no +(E, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "E?", "ConvE"))
                {
                    flag = 0;
                }
            }

            result += flag;

            //r = toint - E.EM1;          // no -(U, E)
            flag = 1;
            e = E.EM1;
            try
            {
                r = toint == e; // no ==(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "ConvInt", "E?"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e > toint; // no >(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "E?", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM1;
            try
            {
                r = e & toint; // no &(E, U)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "E?", "ConvInt"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            e = E.EM3;
            try
            {
                r = toint ^ e; // no ^(U, E)
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "ConvInt", "E?"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enumhelper.enumhelper
{
    // <Area>Enum -- binary operator</Area>
    // <Title>Predefined enum binary operators</Title>
    // <Description>
    // Dependency: casting from dynamic enum object to it's underlying type can work
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>
    using System;

    public enum EInt
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5,
        EM6,
        EM7
    }

    public enum EByte : byte
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5,
        EM6,
        EM7
    }

    public class Helper
    {
        public static int result = 0;
        public static void ExecPositiveTest<T>(dynamic dobj, Type exp_type, T exp_undervalue, string tip, Func<dynamic, dynamic> test) where T : struct
        {
            int flag = 1;
            try
            {
                dynamic dr = test(dobj);
                if ((dr.GetType() == exp_type) && (((T)dr).Equals(exp_undervalue)))
                {
                    flag = 0;
                }
                else
                {
                    System.Console.WriteLine("Got invalid result when testing {0}: {1}[{2}]", tip, dr, dr.GetType());
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Catch an unexpected exception when testing {0}: {1}", tip, ex);
            }

            result += flag;
        }

        public static void ExecNegativeTestWithBadOps(dynamic dobj, string[] exp_msg, string tip, Func<dynamic, dynamic> test)
        {
            int flag = 1;
            try
            {
                dynamic dr = test(dobj);
                System.Console.WriteLine("Got invalid result when testing {0}: {1}[{2}]", tip, dr, dr.GetType());
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, exp_msg))
                {
                    flag = 0;
                }
                else
                {
                    System.Console.WriteLine("Catch an unexpected RuntimeBinderException when testing {0}: {1}", tip, ex);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Catch an unexpected exception when testing {0}: {1}", tip, ex);
            }

            result += flag;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum005.enum005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enumhelper.enumhelper;
    // <Area>Enum -- binary operator</Area>
    // <Title>Predefined enum binary operators</Title>
    // <Description>
    // Dependency: casting from dynamic enum object to it's underlying type can work
    //
    // E +(E, U)
    // E +(U, E)
    //
    // U -(E, E)
    // E -(E, U)
    //
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dobj = EInt.EM2;
            var te = typeof(EInt);
            var tint = typeof(int);
            int i1 = 1;
            int in1 = -1;
            EInt ei5 = EInt.EM5;
            int? ni1 = 1;
            int? nin1 = -1;
            EInt? nei0 = EInt.EM0;
            EByte? neb0 = EByte.EM0;
            ulong? nul1 = 1;
            Helper.result = 0;
            #region operator+
            Helper.ExecPositiveTest(dobj, te, 3, "EInt + int(C)", (Func<dynamic, dynamic>)(d => d + 1));
            Helper.ExecPositiveTest(dobj, te, 2, "EInt + int(C0)", (Func<dynamic, dynamic>)(d => d + 0));
            Helper.ExecPositiveTest(dobj, te, 1, "EInt + int", (Func<dynamic, dynamic>)(d => d + in1));
            Helper.ExecPositiveTest(dobj, te, 1, "int(C) + EInt", (Func<dynamic, dynamic>)(d => (-1) + d));
            Helper.ExecPositiveTest(dobj, te, 2, "int(C0) + EInt", (Func<dynamic, dynamic>)(d => 0 + d));
            Helper.ExecPositiveTest(dobj, te, 3, "int + EInt", (Func<dynamic, dynamic>)(d => i1 + d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "EInt", "EInt"
            }

            , "EInt + EInt", (Func<dynamic, dynamic>)(d => d + EInt.EM0));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "EInt", "EByte"
            }

            , "EInt + EByte", (Func<dynamic, dynamic>)(d => d + EByte.EM0));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "EInt", "ulong"
            }

            , "EInt + ulong", (Func<dynamic, dynamic>)(d => d + 1UL));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "double", "EInt"
            }

            , "double + EInt", (Func<dynamic, dynamic>)(d => 1.0 + d));
            // nullable
            Helper.ExecPositiveTest(dobj, te, 3, "EInt + int?", (Func<dynamic, dynamic>)(d => d + ni1));
            Helper.ExecPositiveTest(dobj, te, 1, "int? + EInt", (Func<dynamic, dynamic>)(d => nin1 + d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "EInt", "EInt?"
            }

            , "EInt + EInt?", (Func<dynamic, dynamic>)(d => d + nei0));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "EInt", "ulong?"
            }

            , "EInt + ulong?", (Func<dynamic, dynamic>)(d => d + nul1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "+", "ulong?", "EInt"
            }

            , "ulong? + EInt", (Func<dynamic, dynamic>)(d => nul1 + d));
            #endregion operator+
            #region operator-
            Helper.ExecPositiveTest(dobj, tint, 2, "EInt(dynamic) - EInt", (Func<dynamic, dynamic>)(d => d - EInt.EM0));
            Helper.ExecPositiveTest(dobj, tint, 3, "EInt - EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 - d));
            Helper.ExecPositiveTest(dobj, te, 1, "EInt - int(C)", (Func<dynamic, dynamic>)(d => d - 1));
            Helper.ExecPositiveTest(dobj, te, 2, "EInt - int(C0)", (Func<dynamic, dynamic>)(d => d - 0));
            Helper.ExecPositiveTest(dobj, te, 3, "EInt - int", (Func<dynamic, dynamic>)(d => d - in1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "EInt", "EByte"
            }

            , "EInt - EByte", (Func<dynamic, dynamic>)(d => d - EByte.EM0));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "EInt", "ulong"
            }

            , "EInt - ulong", (Func<dynamic, dynamic>)(d => d - 1UL));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "double", "EInt"
            }

            , "double - EInt", (Func<dynamic, dynamic>)(d => 1.0 - d));

            Helper.ExecPositiveTest(dobj, te, 3, "int - EInt", (Func<dynamic, dynamic>)(d => 5 - d));
            Helper.ExecPositiveTest(dobj, te, -1, "int - EInt", (Func<dynamic, dynamic>)(d => i1 - d));
            Helper.ExecPositiveTest(dobj, tint, -2, "int(C0) - EInt", (Func<dynamic, dynamic>)(d => 0 - d));
            // nullable
            Helper.ExecPositiveTest(dobj, tint, 1, "EInt - Eint?(C)", (Func<dynamic, dynamic>)(d => d - (EInt?)EInt.EM1));
            Helper.ExecPositiveTest(dobj, tint, 2, "EInt - Eint?", (Func<dynamic, dynamic>)(d => d - nei0));
            Helper.ExecPositiveTest(dobj, te, 3, "EInt - int?", (Func<dynamic, dynamic>)(d => d - nin1));

            Helper.ExecPositiveTest(dobj, te, -1, "int? - EInt", (Func<dynamic, dynamic>)(d => ni1 - d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "EInt", "EByte?"
            }

            , "EInt - EByte?", (Func<dynamic, dynamic>)(d => d - neb0));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "EInt", "ulong?"
            }

            , "EInt - ulong?", (Func<dynamic, dynamic>)(d => d - nul1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "-", "ulong?", "EInt"
            }

            , "ulong? - EInt", (Func<dynamic, dynamic>)(d => nul1 - d));
            #endregion operator-
            return Helper.result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum005b.enum005b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enumhelper.enumhelper;
    // <Area>Enum -- binary operator</Area>
    // <Title>Predefined enum binary operators</Title>
    // <Description>
    // Dependency: casting from dynamic enum object to it's underlying type can work
    //
    // bool ==(E, E)
    // bool !=(E, E)
    // bool <(E, E)
    // bool >(E, E)
    // bool <=(E, E)
    // bool >=(E, E)
    //
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>
    //<Expects Status=warning>\(32,16\).*CS0219</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dobj = EInt.EM2;
            var tbool = typeof(bool);
            int i1 = 1;
            EInt ei5 = EInt.EM5;
            int? ni1 = 1;
            EInt? nei5 = EInt.EM5;
            EByte? neb0 = EByte.EM0;
            Helper.result = 0;
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) == EInt(C)", (Func<dynamic, dynamic>)(d => d == EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt == EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 == d));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt(C) != EInt((dynamic))", (Func<dynamic, dynamic>)(d => EInt.EM2 != d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) != EInt", (Func<dynamic, dynamic>)(d => d != ei5));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt(dynamic) > EInt(C)", (Func<dynamic, dynamic>)(d => d > EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt > EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 > d));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt(C) < EInt((dynamic))", (Func<dynamic, dynamic>)(d => EInt.EM2 < d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) < EInt", (Func<dynamic, dynamic>)(d => d < ei5));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) >= EInt(C)", (Func<dynamic, dynamic>)(d => d >= EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt >= EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 >= d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(C) <= EInt((dynamic))", (Func<dynamic, dynamic>)(d => EInt.EM2 <= d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) <= EInt", (Func<dynamic, dynamic>)(d => d <= ei5));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "==", "EInt", "int"
            }

            , "EInt == int", (Func<dynamic, dynamic>)(d => d == i1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "==", "EByte", "EInt"
            }

            , "EByte == EInt", (Func<dynamic, dynamic>)(d => EByte.EM0 == d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "!=", "int", "EInt"
            }

            , "int != EInt", (Func<dynamic, dynamic>)(d => i1 != d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "!=", "EInt", "EByte"
            }

            , "EInt != EByte", (Func<dynamic, dynamic>)(d => d != EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">", "EInt", "int"
            }

            , "EInt > int", (Func<dynamic, dynamic>)(d => d > i1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">", "EByte", "EInt"
            }

            , "EByte > EInt", (Func<dynamic, dynamic>)(d => EByte.EM0 > d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<", "int", "EInt"
            }

            , "int < EInt", (Func<dynamic, dynamic>)(d => i1 < d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<", "EInt", "EByte"
            }

            , "EInt < EByte", (Func<dynamic, dynamic>)(d => d < EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">=", "EInt", "int"
            }

            , "EInt >= int", (Func<dynamic, dynamic>)(d => d >= i1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">=", "EByte", "EInt"
            }

            , "EByte >= EInt", (Func<dynamic, dynamic>)(d => EByte.EM0 >= d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<=", "int", "EInt"
            }

            , "int <= EInt", (Func<dynamic, dynamic>)(d => i1 <= d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<=", "EInt", "EByte"
            }

            , "EInt <= EByte", (Func<dynamic, dynamic>)(d => d <= EByte.EM1));
            // nullable
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) == EInt?(C)", (Func<dynamic, dynamic>)(d => d == (EInt?)EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt? == EInt(dynamic)", (Func<dynamic, dynamic>)(d => nei5 == d));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt?(C) != EInt((dynamic))", (Func<dynamic, dynamic>)(d => (EInt?)EInt.EM2 != d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) != EInt?", (Func<dynamic, dynamic>)(d => d != nei5));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt(dynamic) > EInt?(C)", (Func<dynamic, dynamic>)(d => d > (EInt?)EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt? > EInt(dynamic)", (Func<dynamic, dynamic>)(d => nei5 > d));
            Helper.ExecPositiveTest(dobj, tbool, false, "EInt?(C) < EInt((dynamic))", (Func<dynamic, dynamic>)(d => (EInt?)EInt.EM2 < d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) < EInt?", (Func<dynamic, dynamic>)(d => d < nei5));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) >= EInt?(C)", (Func<dynamic, dynamic>)(d => d >= (EInt?)EInt.EM2));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt? >= EInt(dynamic)", (Func<dynamic, dynamic>)(d => nei5 >= d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt?(C) <= EInt((dynamic))", (Func<dynamic, dynamic>)(d => (EInt?)EInt.EM2 <= d));
            Helper.ExecPositiveTest(dobj, tbool, true, "EInt(dynamic) <= EInt?", (Func<dynamic, dynamic>)(d => d <= nei5));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "==", "EInt", "int?"
            }

            , "EInt == int?", (Func<dynamic, dynamic>)(d => d == ni1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "==", "EByte?", "EInt"
            }

            , "EByte? == EInt", (Func<dynamic, dynamic>)(d => (EByte?)EByte.EM0 == d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "!=", "int?", "EInt"
            }

            , "int? != EInt", (Func<dynamic, dynamic>)(d => ni1 != d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "!=", "EInt", "EByte?"
            }

            , "EInt != EByte?", (Func<dynamic, dynamic>)(d => d != (EByte?)EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">", "EInt", "int?"
            }

            , "EInt > int?", (Func<dynamic, dynamic>)(d => d > ni1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">", "EByte?", "EInt"
            }

            , "EByte? > EInt", (Func<dynamic, dynamic>)(d => (EByte?)EByte.EM0 > d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<", "int?", "EInt"
            }

            , "int? < EInt", (Func<dynamic, dynamic>)(d => ni1 < d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<", "EInt", "EByte?"
            }

            , "EInt < EByte?", (Func<dynamic, dynamic>)(d => d < (EByte?)EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">=", "EInt", "int?"
            }

            , "EInt >= int?", (Func<dynamic, dynamic>)(d => d >= ni1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            ">=", "EByte?", "EInt"
            }

            , "EByte? >= EInt", (Func<dynamic, dynamic>)(d => (EByte?)EByte.EM0 >= d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<=", "int?", "EInt"
            }

            , "int? <= EInt", (Func<dynamic, dynamic>)(d => ni1 <= d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "<=", "EInt", "EByte?"
            }

            , "EInt <= EByte?", (Func<dynamic, dynamic>)(d => d <= (EByte?)EByte.EM1));
            return Helper.result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enum005c.enum005c
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.enumhelper.enumhelper;
    // <Area>Enum -- binary operator</Area>
    // <Title>Predefined enum binary operators</Title>
    // <Description>
    // Dependency: casting from dynamic enum object to it's underlying type can work
    //
    // E &(E, E)
    // E |(E, E)
    // E ^(E, E)
    //
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dobj = EInt.EM2;
            var teint = typeof(EInt);
            int i1 = 1;
            EInt ei5 = EInt.EM5;
            int? ni1 = 1;
            EInt? nei5 = EInt.EM5;
            Helper.result = 0;
            Helper.ExecPositiveTest(dobj, teint, 2, "EInt(dynamic) & EInt(C)", (Func<dynamic, dynamic>)(d => d & EInt.EM3));
            Helper.ExecPositiveTest(dobj, teint, 0, "EInt & EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 & d));
            Helper.ExecPositiveTest(dobj, teint, 3, "EInt(C) | EInt((dynamic))", (Func<dynamic, dynamic>)(d => EInt.EM1 | d));
            Helper.ExecPositiveTest(dobj, teint, 7, "EInt(dynamic) | EInt", (Func<dynamic, dynamic>)(d => d | ei5));
            Helper.ExecPositiveTest(dobj, teint, 1, "EInt(dynamic) ^ EInt(C)", (Func<dynamic, dynamic>)(d => d ^ EInt.EM3));
            Helper.ExecPositiveTest(dobj, teint, 7, "EInt ^ EInt(dynamic)", (Func<dynamic, dynamic>)(d => ei5 ^ d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "&", "EInt", "int"
            }

            , "EInt & int", (Func<dynamic, dynamic>)(d => d & i1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "&", "EByte", "EInt"
            }

            , "EByte & EInt", (Func<dynamic, dynamic>)(d => EByte.EM0 & d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "|", "int", "EInt"
            }

            , "int | EInt", (Func<dynamic, dynamic>)(d => i1 | d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "|", "EInt", "EByte"
            }

            , "EInt | EByte", (Func<dynamic, dynamic>)(d => d | EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "^", "EInt", "int"
            }

            , "EInt ^ int", (Func<dynamic, dynamic>)(d => d ^ i1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "^", "EByte", "EInt"
            }

            , "EByte ^ EInt", (Func<dynamic, dynamic>)(d => EByte.EM0 ^ d));
            // nullable
            Helper.ExecPositiveTest(dobj, teint, 2, "EInt(dynamic) & EInt?(C)", (Func<dynamic, dynamic>)(d => d & (EInt?)EInt.EM3));
            Helper.ExecPositiveTest(dobj, teint, 0, "EInt? & EInt(dynamic)", (Func<dynamic, dynamic>)(d => nei5 & d));
            Helper.ExecPositiveTest(dobj, teint, 3, "EInt?(C) | EInt((dynamic))", (Func<dynamic, dynamic>)(d => (EInt?)EInt.EM1 | d));
            Helper.ExecPositiveTest(dobj, teint, 7, "EInt(dynamic) | EInt?", (Func<dynamic, dynamic>)(d => d | nei5));
            Helper.ExecPositiveTest(dobj, teint, 1, "EInt(dynamic) ^ EInt?(C)", (Func<dynamic, dynamic>)(d => d ^ (EInt?)EInt.EM3));
            Helper.ExecPositiveTest(dobj, teint, 7, "EInt? ^ EInt(dynamic)", (Func<dynamic, dynamic>)(d => nei5 ^ d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "&", "EInt", "int?"
            }

            , "EInt & int?", (Func<dynamic, dynamic>)(d => d & ni1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "&", "EByte?", "EInt"
            }

            , "EByte? & EInt", (Func<dynamic, dynamic>)(d => (EByte?)EByte.EM0 & d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "|", "int?", "EInt"
            }

            , "int? | EInt", (Func<dynamic, dynamic>)(d => ni1 | d));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "|", "EInt", "EByte?"
            }

            , "EInt | EByte?", (Func<dynamic, dynamic>)(d => d | (EByte?)EByte.EM1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "^", "EInt", "int?"
            }

            , "EInt ^ int?", (Func<dynamic, dynamic>)(d => d ^ ni1));
            Helper.ExecNegativeTestWithBadOps(dobj, new[]
            {
            "^", "EByte?", "EInt"
            }

            , "EByte? ^ EInt", (Func<dynamic, dynamic>)(d => (EByte?)EByte.EM0 ^ d));
            return Helper.result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.basic.bug811970.bug811970
{
    // <Area>dynamic</Area>
    // <Title>conversion</Title>
    // <Description>
    //   Array indices do not consider non-int conversion operators
    // </Description>
    // <Related Bugs></Related Bugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public static implicit operator ulong (A x)
        {
            return 1;
        }

        public static implicit operator long (A x)
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
            var x = new A();
            int result = new[]
            {
            1
            }

            [x];
            result += new[]
            {
            1
            }

            [(dynamic)x];
            if (result == 2)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}
