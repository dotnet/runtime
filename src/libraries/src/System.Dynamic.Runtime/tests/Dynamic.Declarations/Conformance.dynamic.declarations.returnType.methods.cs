// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.genericmethod001.genericmethod001
{
    public class Test
    {
        private class MyClass
        {
            public dynamic Foo<T>()
            {
                dynamic d = default(T);
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                mc.Foo<int>().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.method001.method001
{
    public class Test
    {
        private class MyClass
        {
            public dynamic Foo()
            {
                dynamic d = new object();
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                mc.Foo().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "object", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.method002.method002
{
    public class Test
    {
        private class MyClass<T>
        {
            public dynamic Foo()
            {
                dynamic d = default(T);
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass<int> mc = new MyClass<int>();
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                mc.Foo().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.method003.method003
{
    public class Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod(null));
            }

            public static int MainMethod(string[] args)
            {
                dynamic d = new Program();
                try
                {
                    var x = d.b(d);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, e.Message))
                        return 0;
                }

                return 1;
            }

            private void b(dynamic d)
            {
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.staticgenericmethod001.staticgenericmethod001
{
    public class Test
    {
        private class MyClass
        {
            public static dynamic Foo<T>()
            {
                dynamic d = default(T);
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                MyClass.Foo<int>().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.staticgenericmethod002.staticgenericmethod002
{
    public class Test
    {
        private class MyClass<T>
        {
            public static dynamic Foo()
            {
                dynamic d = default(T);
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                MyClass<int>.Foo().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.staticmethod001.staticmethod001
{
    public class Test
    {
        private class MyClass
        {
            public static dynamic Foo()
            {
                dynamic d = new object();
                return d;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                MyClass.Foo().Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "object", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.methods.staticmethod002.staticmethod002
{
    static // <Title> Having dynamic as a return type</Title>
           // <Description>
           // </Description>
           // <RelatedBugs></RelatedBugs>
           //<Expects Status=success></Expects>
           // <Code>
public class MyClass
    {
        public static dynamic Foo(this int x)
        {
            dynamic d = x;
            return d;
        }
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
            int x = 3;
            dynamic d = x.Foo();
            try
            {
                //This should not compile if the return type of Foo is not dynamic
                d.Bar();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Bar"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}
