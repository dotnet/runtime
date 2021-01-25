// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.field01.field01
{
    // <Title> Compound operator in readonly field.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                char c = (char)2;
                d.field *= c;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, e.Message))
                    return 0;
            }

            return 1;
        }

        public readonly long field = 10;
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.field02.field02
{
    // <Title> Compound operator non-delegate += delegate.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                d.field += new MyDel(Method);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "+=", "int", "Test.MyDel"))
                    return 0;
            }

            return 1;
        }

        public int field = 0;
        public static int Method(int i)
        {
            return i;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.property01.property01
{
    // <Title> Compound operator in readonly property.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d.Field += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AssgReadonlyProp, e.Message, "Test.Field"))
                    return 0;
            }

            return 1;
        }

        public string Field
        {
            get
            {
                return "A";
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.property02.property02
{
    // <Title> Compound operator in readonly property.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TestClass
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d.Field += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "Test.Field"))
                    return 0;
            }

            return 1;
        }
    }

    public class Test
    {
        public string Field
        {
            get
            {
                return "A";
            }

            private set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.property03.property03
{
    // <Title> Compound operator in readonly property.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d.Field += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.PropertyLacksGet, e.Message, "Test.Field"))
                    return 0;
            }

            return 1;
        }

        public string Field
        {
            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.property04.property04
{
    // <Title> Compound operator: property += delegate.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            d.Field += new MyDel(t.Method); // No exception: string + delegate
            try
            {
                d.FieldInt -= new MyDel(t.Method);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "int", "Test.MyDel"))
                    return 0;
            }

            return 1;
        }

        public string Field
        {
            get
            {
                return "A";
            }

            set
            {
            }
        }

        public int FieldInt
        {
            get
            {
                return 10;
            }

            set
            {
            }
        }

        public int Method(int i)
        {
            return i;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.indexer01.indexer01
{
    // <Title> Compound operator in readonly indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d[10] += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AssgReadonlyProp, e.Message, "Test.this[long]"))
                    return 0;
            }

            return 1;
        }

        public string this[long s]
        {
            get
            {
                return "A";
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.indexer02.indexer02
{
    // <Title> Compound operator in readonly indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TestClass
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d[10] += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "Test.this[long]"))
                    return 0;
            }

            return 1;
        }
    }

    public class Test
    {
        public string this[long s]
        {
            get
            {
                return "A";
            }

            private set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.indexer03.indexer03
{
    // <Title> Compound operator in readonly indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                byte b = 10;
                d[10] += b;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.PropertyLacksGet, e.Message, "Test.this[long]"))
                    return 0;
            }

            return 1;
        }

        public string this[long s]
        {
            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.negative.indexer04.indexer04
{
    // <Title> Compound operator indexer += delegate.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            try
            {
                d[10] += new MyDel(delegate (int x)
                {
                    return x;
                }

                );
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "+=", "int", "Test.MyDel"))
                    return 0;
            }

            return 1;
        }

        public int this[long s]
        {
            get
            {
                return 10;
            }

            set
            {
            }
        }
    }
    //</Code>
}
