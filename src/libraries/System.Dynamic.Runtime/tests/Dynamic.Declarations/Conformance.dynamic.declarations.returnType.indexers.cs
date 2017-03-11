// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer001.indexer001
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public dynamic this[int x]
            {
                get
                {
                    dynamic d = 4;
                    MyClass.Status = 1;
                    return d;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if ((int)value == 5)
                        MyClass.Status = 2;
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
            dynamic d = mc[3];
            if ((int)d != 4 || MyClass.Status != 1)
                return 1;
            mc[3] = 5;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer002.indexer002
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public dynamic this[dynamic x]
            {
                get
                {
                    MyClass.Status = 1;
                    return x;
                }

                set
                {
                    if ((string)value == "Foo")
                        MyClass.Status = 2;
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
            dynamic d = mc[3];
            if ((int)d != 3 || MyClass.Status != 1)
                return 1;
            mc[3] = "Foo";
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer003.indexer003
{
    public class Test
    {
        public class C
        {
            public int Field;
        }

        public class MyClass
        {
            public static int Status;
            public dynamic this[C x]
            {
                get
                {
                    MyClass.Status = 1;
                    return x;
                }

                set
                {
                    if (((C)value).Field == 4)
                        MyClass.Status = 2;
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
            C c = new C();
            dynamic d = mc[c];
            if (d != c || MyClass.Status != 1)
                return 1;
            mc[c] = new C()
            {
                Field = 4
            }

            ;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer004.indexer004
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public dynamic this[float? x]
            {
                get
                {
                    MyClass.Status = 1;
                    return 4.5f;
                }

                set
                {
                    if ((int)value == 4)
                        MyClass.Status = 2;
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
            dynamic d = mc[null];
            if ((float)d != 4.5f || MyClass.Status != 1)
                return 1;
            mc[4.5f] = 4;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer008.indexer008
{
    public class Test
    {
        public class MyClass
        {
            public static int Status;
            public dynamic this[int x, dynamic d]
            {
                get
                {
                    MyClass.Status = 1;
                    return d;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if ((int)value == 5)
                        MyClass.Status = 2;
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
            dynamic d = mc[3, 4];
            if ((int)d != 4 || MyClass.Status != 1)
                return 1;
            mc[3, d] = 5;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer009.indexer009
{
    public class Test
    {
        public class MyClass
        {
            public static int Status;
            public dynamic this[int x, dynamic d]
            {
                get
                {
                    MyClass.Status = 1;
                    return d;
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
            dynamic d = mc[3, 4];
            if ((int)d != 4 || MyClass.Status != 1)
                return 1;
            try
            {
                mc[3, d] = 5;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AssgReadonlyProp, e.Message, "Test.MyClass.this[int, object]"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.indexers.indexer010.indexer010
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public dynamic this[int x, dynamic d]
            {
                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if ((int)value == 5)
                        MyClass.Status = 2;
                }

                get
                {
                    return 4;
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
            mc[3, null] = 5;
            if (MyClass.Status != 2)
                return 1;
            try
            {
                dynamic d = mc[3, 4];
                d.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}
