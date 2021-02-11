// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke001.invoke001
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public void Do(int x)
            {
                Test.s_status = true;
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
            dynamic d = (myDel)mc.Do;
            d(3); //We invoke the dynamic delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke002.invoke002
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public static void Do(int x)
            {
                Test.s_status = true;
            }

            public myDel Prop
            {
                get
                {
                    return new myDel(MyClass.Do);
                }
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
            dynamic d = mc.Prop;
            d(3); //We invoke the dynamic delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke003.invoke003
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        public class MyClass
        {
            public static void Do(int x)
            {
                System.Console.WriteLine("Do");
            }

            public myDel Prop
            {
                get
                {
                    return new myDel(MyClass.Do);
                }
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
                dynamic d = new MyClass();
                dynamic del = d.Prop(3); //Property returning a delegate
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke004.invoke004
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        public class MyClass
        {
            public static void Do(int x)
            {
                Test.s_status = true;
            }

            public myDel Prop
            {
                get
                {
                    return new myDel(MyClass.Do);
                }
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new MyClass();
            dynamic del = d.Prop;
            del(3);
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke005.invoke005
{
    public class Test
    {
        public delegate void myDel(int x);
        public class MyClass
        {
            public static void Do(int x)
            {
                System.Console.WriteLine("Do");
            }

            public myDel Method()
            {
                return new myDel(MyClass.Do);
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
                dynamic d = new MyClass();
                dynamic del = d.Method()(3); //Method returning a delegate
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke006.invoke006
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public void Do(int x)
            {
                Test.s_status = true;
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
            myDel del = mc.Do;
            dynamic d = del;
            d(3); //We invoke the dynamic delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke007.invoke007
{
    public class Test
    {
        // static bool Status = false;
        public delegate int myDel(int x);
        private class MyClass
        {
            public int Do(int p)
            {
                // Test.Status = true;
                System.Console.WriteLine("DoInt");
                return 1;
            }

            public short Do(short x)
            {
                // Test.Status = false;
                System.Console.WriteLine("DoShort");
                return 2;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic mc = new MyClass();
            try
            {
                myDel del = mc.Do;
                del(3); //We invoke the dynamic delegate
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindPropertyFailedMethodGroup, ex.Message, "Do");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke008.invoke008
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public void Do(int x)
            {
                Test.s_status = true;
            }

            public void Do(short x)
            {
                Test.s_status = false;
                System.Console.WriteLine("Do");
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
            dynamic d = (myDel)mc.Do;
            d(3); //We invoke the dynamic delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke009.invoke009
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        public class MyClass
        {
            public static void Do(int x)
            {
                Test.s_status = true;
            }

            public myDel Prop
            {
                get
                {
                    return new myDel(MyClass.Do);
                }
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new MyClass();
            dynamic del = (myDel)d.Prop; //Property returning a delegate
            del(3); //We invoke the delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke010.invoke010
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        public class MyClass
        {
            public static void Do(int x)
            {
                Test.s_status = true;
            }

            public myDel Method()
            {
                return new myDel(MyClass.Do);
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new MyClass();
            dynamic del = (myDel)d.Method(); //Method returning a delegate
            del(3); //Invoke the delegate
            return Test.s_status ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke011.invoke011
{
    // <Title> Invoking dynamic </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        public Action Invoke = () => System.Console.WriteLine("A");
    }

    public class P
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic y = new A();
            try
            {
                var x = y();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindInvokeFailedNonDelegate, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.invoke012.invoke012
{
    // <Title> Invoking dynamic </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public void Invoke()
        {
            System.Console.WriteLine("A");
        }
    }

    public class P
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic y = new A();
            try
            {
                var x = y();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindInvokeFailedNonDelegate, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.neginvoke001.neginvoke001
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public static void Do(int x)
            {
                Test.s_status = true;
                System.Console.WriteLine("Do");
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new MyClass();
            dynamic del = null;
            try
            {
                d = d.Prop(3); //Property returning a delegate
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "Test.MyClass", "Prop");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.invokeDynamic.neginvoke002.neginvoke002
{
    public class Test
    {
        private static bool s_status = false;
        public delegate void myDel(int x);
        private class MyClass
        {
            public void Do(int x)
            {
                Test.s_status = true;
                System.Console.WriteLine("Do");
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
            dynamic d = (myDel)mc.Do;
            try
            {
                d(3, 4, 6); //We invoke the dynamic delegate
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadDelArgCount, ex.Message, "myDel", "3");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}
