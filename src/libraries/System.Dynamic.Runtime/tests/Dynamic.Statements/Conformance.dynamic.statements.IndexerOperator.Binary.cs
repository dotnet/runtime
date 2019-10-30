// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.verify.verify;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.verify.verify
{
    public class Verify
    {
        public static int Check(dynamic actual, dynamic expected)
        {
            int index = 0;
            foreach (var item in expected)
            {
                if (actual[index] != expected[index])
                    return 0;
                index++;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.add001.add001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> + </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] + 2;
            result[1] = 2 + t[1];
            dynamic index = 1;
            result[2] = t[index] + 2;
            result[3] = 2 + t[index];
            result[4] = t[1] + t[index];
            result[5] = t[index] + t[1];
            return Verify.Check(result, new int[]
            {
            3, 3, 3, 3, 2, 2
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.add002.add002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> + </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] + 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] + 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.and001.and001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>&&</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[bool index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            bool i = true, T = true, F = false;
            result[0] = t[i] && T;
            result[1] = F && t[i];
            dynamic index = false;
            result[2] = t[index] && T;
            result[3] = F && t[index];
            result[4] = t[i] && t[index];
            result[5] = t[index] && t[i];
            return Verify.Check(result, new bool[]
            {
            true, false, false, false, false, false
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.and002.and002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> &&  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 2;
            try
            {
                var x1 = t[1] && true;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "int", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] && t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "int", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment001.assignment001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> += </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[1] += 2;
            result[0] = t.flag;
            dynamic index = 1;
            t[index] += 2;
            result[1] = t.flag;
            t[1] += t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            3, 3, 2
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment0010e.assignment0010e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >>=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] >>= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">>=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] >>= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">>=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment001e.assignment001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> +=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] += 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] += 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment002.assignment002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> -= </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[1] -= 2;
            result[0] = t.flag;
            dynamic index = 1;
            t[index] -= 2;
            result[1] = t.flag;
            t[1] -= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            -1, -1, 0
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment002e.assignment002e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> -=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] -= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] -= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment003.assignment003
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> *= </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[1] *= 2;
            result[0] = t.flag;
            dynamic index = 1;
            t[index] *= 2;
            result[1] = t.flag;
            t[1] *= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            2, 2, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment003e.assignment003e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> *=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] *= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] *= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment004.assignment004
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> /= </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[6] /= 2;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] /= 2;
            result[1] = t.flag;
            t[1] /= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            3, 4, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment004e.assignment004e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> /=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] /= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] /= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment005.assignment005
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  %=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[6] %= 2;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] %= 2;
            result[1] = t.flag;
            t[1] %= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            0, 1, 0
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment005e.assignment005e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> %=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] %= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] %= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment006.assignment006
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  &=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[6] &= 2;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] &= 2;
            result[1] = t.flag;
            t[1] &= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            2, 0, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment006e.assignment006e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> &=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] &= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] &= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment007.assignment007
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  |=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[6] |= 2;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] |= 2;
            result[1] = t.flag;
            t[1] |= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            6, 11, 9
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment007e.assignment007e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> |=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] |= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] |= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment008.assignment008
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  ^=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            t[6] ^= 2;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] ^= 2;
            result[1] = t.flag;
            t[1] ^= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            4, 11, 8
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment008e.assignment008e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> ^=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] ^= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] ^= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment009.assignment009
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  >>=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[3];
            t[6] >>= 1;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] >>= 2;
            result[1] = t.flag;
            t[1] >>= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            3, 2, 0
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment009e.assignment009e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> <<=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] <<= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<<=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] <<= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<<=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.assignment010.assignment010
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>  <<=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[3];
            t[6] <<= 1;
            result[0] = t.flag;
            dynamic index = 9;
            t[index] <<= 2;
            result[1] = t.flag;
            t[1] <<= t[index];
            result[2] = t.flag;
            return Verify.Check(result, new int[]
            {
            12, 36, 512
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.devide001.devide001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>/</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[10] / 2;
            result[1] = 6 / t[3];
            dynamic index = 8;
            result[2] = t[index] / 2;
            result[3] = 24 / t[index];
            result[4] = t[16] / t[index];
            result[5] = t[index] / t[2];
            return Verify.Check(result, new int[]
            {
            5, 2, 4, 3, 2, 4
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.devide002.devide002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> / </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] / 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] / 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.equal001.equal001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> == </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] == 2;
            result[1] = 2 == t[1];
            dynamic index = 1;
            result[2] = t[index] == 2;
            result[3] = 2 == t[index];
            result[4] = t[1] == t[index];
            result[5] = t[index] == t[1];
            return Verify.Check(result, new bool[]
            {
            false, false, false, false, true, true
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.equal002.equal002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> ==  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 2;
            try
            {
                var x1 = t[1] == "";
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "int", "string");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] == "1";
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "int", "string");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.greater001.greater001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] > 2;
            result[1] = 2 > t[1];
            dynamic index = 1;
            result[2] = t[index] > 2;
            result[3] = 2 > t[index];
            result[4] = t[1] > t[index];
            result[5] = t[index] > t[1];
            return Verify.Check(result, new bool[]
            {
            false, true, false, true, false, false
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.greater002.greater002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] > 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] > 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.greaterequal001.greaterequal001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >= </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] >= 2;
            result[1] = 2 >= t[1];
            dynamic index = 1;
            result[2] = t[index] >= 2;
            result[3] = 2 >= t[index];
            result[4] = t[1] >= t[index];
            result[5] = t[index] >= t[1];
            return Verify.Check(result, new bool[]
            {
            false, true, false, true, true, true
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.greaterequal002.greaterequal002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] >= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] >= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.is001.is001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> is </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(36,21\).*CS1981</Expects>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] is int;
            result[1] = t[1] is object;
            dynamic index = 1;
            result[2] = t[index] is int;
            result[3] = t[index] is dynamic;
            result[4] = t[index] is string;
            result[5] = t[index] is Test;
            return Verify.Check(result, new bool[]
            {
            true, true, true, true, false, false
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.leftshift001.leftshift001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> << </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] << 2;
            result[1] = 2 << t[1];
            dynamic index = 1;
            result[2] = t[index] << 2;
            result[3] = 2 << t[index];
            result[4] = t[1] << t[index];
            result[5] = t[index] << t[1];
            return Verify.Check(result, new int[]
            {
            4, 4, 4, 4, 2, 2
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.leftshift002.leftshift002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> <<  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] << 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<<", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] << 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<<", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.less001.less001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> < </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] < 2;
            result[1] = 2 < t[1];
            dynamic index = 1;
            result[2] = t[index] < 2;
            result[3] = 2 < t[index];
            result[4] = t[1] < t[index];
            result[5] = t[index] < t[1];
            return Verify.Check(result, new bool[]
            {
            true, false, true, false, false, false
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.less002.less002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> <  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] < 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] < 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.lessequal001.lessequal001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> <= </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] <= 2;
            result[1] = 2 <= t[1];
            dynamic index = 1;
            result[2] = t[index] <= 2;
            result[3] = 2 <= t[index];
            result[4] = t[1] <= t[index];
            result[5] = t[index] <= t[1];
            return Verify.Check(result, new bool[]
            {
            true, false, true, false, true, true
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.lessequal002.lessequal002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> <=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] <= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<=", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] <= 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "<=", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicaland001.logicaland001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> & </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] & 2;
            result[1] = 2 & t[1];
            dynamic index = 1;
            result[2] = t[index] & 2;
            result[3] = 2 & t[index];
            result[4] = t[1] & t[index];
            result[5] = t[index] & t[1];
            return Verify.Check(result, new int[]
            {
            0, 0, 0, 0, 1, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicaland002.logicaland002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> &  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] & 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] & 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicalor001.logicalor001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> | </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] | 2;
            result[1] = 2 | t[1];
            dynamic index = 1;
            result[2] = t[index] | 2;
            result[3] = 2 | t[index];
            result[4] = t[1] | t[index];
            result[5] = t[index] | t[1];
            return Verify.Check(result, new int[]
            {
            3, 3, 3, 3, 1, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicalor002.logicalor002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> |  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] | 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] | 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicalxor001.logicalxor001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> ^ </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] ^ 2;
            result[1] = 2 ^ t[1];
            dynamic index = 1;
            result[2] = t[index] ^ 2;
            result[3] = 2 ^ t[index];
            result[4] = t[1] ^ t[index];
            result[5] = t[index] ^ t[1];
            return Verify.Check(result, new int[]
            {
            3, 3, 3, 3, 0, 0
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.logicalxor002.logicalxor002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> ^  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] ^ 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] ^ 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.multi001.multi001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>*</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] * 2;
            result[1] = 2 * t[1];
            dynamic index = 1;
            result[2] = t[index] * 2;
            result[3] = 2 * t[index];
            result[4] = t[1] * t[index];
            result[5] = t[index] * t[1];
            return Verify.Check(result, new int[]
            {
            2, 2, 2, 2, 1, 1
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.multi002.multi002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> *  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] * 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] * 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.notequal001.notequal001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> != </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            result[0] = t[1] != 2;
            result[1] = 2 != t[1];
            dynamic index = 1;
            result[2] = t[index] != 2;
            result[3] = 2 != t[index];
            result[4] = t[1] != t[index];
            result[5] = t[index] != t[1];
            return Verify.Check(result, new bool[]
            {
            true, true, true, true, false, false
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.notequal002.notequal002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> !=  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 2;
            try
            {
                var x1 = t[1] != "";
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "!=", "int", "string");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] != "1";
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "!=", "int", "string");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.or001.or001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>||</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[bool index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            bool[] result = new bool[6];
            bool i = true, T = true, F = false;
            result[0] = t[i] || T;
            result[1] = F || t[i];
            dynamic index = false;
            result[2] = t[index] || T;
            result[3] = F || t[index];
            result[4] = t[i] || t[index];
            result[5] = t[index] || t[i];
            return Verify.Check(result, new bool[]
            {
            true, true, true, false, true, true
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.or002.or002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> ||  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 2;
            try
            {
                var x1 = t[1] || true;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "int", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] || t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "int", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.remainder001.remainder001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>%</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[10] % 3;
            result[1] = 6 % t[4];
            dynamic index = 8;
            result[2] = t[index] % 3;
            result[3] = 12 % t[index];
            result[4] = t[12] % t[index];
            result[5] = t[index] % t[2];
            return Verify.Check(result, new int[]
            {
            1, 2, 2, 4, 4, 0
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.remainder002.remainder002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> %  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] % 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] % 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.rightshift001.rightshift001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >> </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[9] >> 2;
            result[1] = 2 >> t[1];
            dynamic index = 16;
            result[2] = t[index] >> 2;
            result[3] = 2 >> t[index];
            result[4] = t[1] >> t[index];
            result[5] = t[index] >> t[1];
            return Verify.Check(result, new int[]
            {
            2, 1, 4, 0, 0, 8
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.rightshift002.rightshift002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> >>  </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] >> 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">>", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] >> 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">>", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.sub001.sub001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description>-</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int flag;
        public int this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int[] result = new int[6];
            result[0] = t[1] - 2;
            result[1] = 2 - t[1];
            dynamic index = 1;
            result[2] = t[index] - 2;
            result[3] = 2 - t[index];
            result[4] = t[1] - t[index];
            result[5] = t[index] - t[1];
            return Verify.Check(result, new int[]
            {
            3, 3, 3, 3, 2, 2
            }

            );
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Binary.sub002.sub002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> binary operator  </Title>
    // <Description> - </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public bool flag;
        public bool this[int index]
        {
            set
            {
                flag = value;
            }

            get
            {
                return index > 5;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            dynamic index = 5;
            try
            {
                var x1 = t[1] - 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "bool", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index] - 1;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "bool", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}
