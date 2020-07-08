// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.Null.nullable01.nullable01
{
    // <Description>Nullable invocation of GetValueOrDefault</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int? x = null;
            dynamic y = 0;
            var val1 = x.GetValueOrDefault((int)y);
            var val2 = x.Equals((int)y) ? 1 : 0;
            return val1 + val2;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.Null.null002.null002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 1;
            Test t = null;
            try
            {
                int i = t.Foo(d);
            }
            //catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            //{
            //    bool ret = ErrorVerifier.Verify(RuntimeErrorId.NullReferenceOnMemberException, ex.Message);
            // Used to be NullReferenceException
            // Better error message - Cannot perform runtime binding on a null reference
            // But hard to validate
            //    if (ret)
            //        return 0;
            //}
            // change from Microsoft.CSharp.RuntimeBinder.RuntimeBinderException to System.NullReferenceException
            catch (System.NullReferenceException)
            {
                return 0;
            }

            return 1;
        }

        public int Foo(dynamic d)
        {
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.Null.null003.null003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 1;
            dynamic t = default(Test);
            try
            {
                int i = t.Foo(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.NullReferenceOnMemberException, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }

        public int Foo(dynamic d)
        {
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.Null.null004.null004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 1;
            dynamic t = new Test();
            try
            {
                int i = t.Bar().Foo(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "Test", "Bar");
                if (ret)
                    return 0;
            }

            return 1;
        }

        public int Foo(dynamic d)
        {
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.Null.void001.void001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 1;
            dynamic t = new Test();
            try
            {
                dynamic i = t.Bar().Foo(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }

        public void Bar()
        {
        }
    }
    // </Code>
}
