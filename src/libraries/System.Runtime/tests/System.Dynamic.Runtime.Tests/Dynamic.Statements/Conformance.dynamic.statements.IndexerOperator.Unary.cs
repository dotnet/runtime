// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.verify.verify
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.decrease001.decrease001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
            int x1 = t[1]--;
            if (x1 != 1 || t.flag != 0)
                return 1;
            dynamic index = 5;
            int x2 = t[index]--;
            if (x2 != 5 || t.flag != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.decrease001e.decrease001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
                int x1 = t[1]--;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "--", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index]++;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "++", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.decrease002.decrease002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
            int x1 = --t[1];
            if (x1 != 0 || t.flag != 0)
                return 1;
            dynamic index = 5;
            int x2 = --t[index];
            if (x2 != 4 || t.flag != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.decrease002e.decrease002e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>postfix decrement</Description>
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
                int x1 = --t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "--", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = --t[index];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "--", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.increase001.increase001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
            int x1 = t[1]++;
            if (x1 != 1 || t.flag != 2)
                return 1;
            dynamic index = 5;
            int x2 = t[index]++;
            if (x2 != 5 || t.flag != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.increase001e.increase001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>postfix increment</Description>
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
                int x1 = t[1]++;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "++", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = t[index]++;
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "++", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.increase002.increase002
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
            int x1 = ++t[1];
            if (x1 != 2 || t.flag != 2)
                return 1;
            dynamic index = 5;
            int x2 = ++t[index];
            if (x2 != 6 || t.flag != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.increase002e.increase002e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>postfix increment</Description>
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
                int x1 = ++t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "++", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = ++t[index];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "++", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.minus001.minus001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>minus</Description>
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
            int x1 = -t[1];
            if (x1 != -1)
                return 1;
            dynamic index = 5;
            int x2 = -t[index];
            if (x2 != -5)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.minus001e.minus001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>minus</Description>
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
                var x1 = -t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "-", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = -t[index];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "-", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.not001.not001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
            bool x1 = !t[true];
            if (x1 != false)
                return 1;
            dynamic index = true;
            bool x2 = !t[index];
            if (x2 != false)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.not001e.not001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>prefix increment</Description>
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
                var x1 = !t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "!", "int");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = !t[index];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "!", "int");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.plus001.plus001
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>minus</Description>
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
            int x1 = +t[1];
            if (x1 != 1)
                return 1;
            dynamic index = 5;
            int x2 = +t[index];
            if (x2 != 5)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.IndexerOperator.Unary.plus001e.plus001e
{
    // <Area>operator on dynamic indexer</Area>
    // <Title> unary operator  </Title>
    // <Description>minus</Description>
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
                var x1 = +t[1];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "+", "bool");
                if (!ret)
                    return 1;
            }

            try
            {
                var x2 = +t[index];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, ex.Message, "+", "bool");
                if (!ret)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}
