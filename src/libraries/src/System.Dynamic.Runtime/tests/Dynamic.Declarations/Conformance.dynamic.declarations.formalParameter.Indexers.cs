// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Indexers.indexer001.indexer001
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public decimal this[dynamic x]
            {
                get
                {
                    MyClass.Status = 1;
                    return 1m;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if (value == decimal.One)
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
            decimal d = mc[null];
            if (d != 1m || MyClass.Status != 1)
                return 1;
            mc[3] = decimal.One;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Indexers.indexer002.indexer002
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public char? this[dynamic x]
            {
                get
                {
                    MyClass.Status = 1;
                    char? c = null;
                    return c;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if (value == char.MinValue)
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
            char? d = mc[null];
            if (d != null || MyClass.Status != 1)
                return 1;
            mc[3] = char.MinValue;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Indexers.indexer003.indexer003
{
    public class Test
    {
        private class C
        {
            public int Field;
        }

        private class MyClass
        {
            public static int Status;
            public C this[dynamic x]
            {
                get
                {
                    MyClass.Status = 1;
                    return new C()
                    {
                        Field = 3
                    }

                    ;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if (value.Field == 3)
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
            C d = mc[1m];
            if (d.Field != 3 || MyClass.Status != 1)
                return 1;
            mc[3] = d;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Indexers.indexer004.indexer004
{
    public class Test
    {
        private class MyClass
        {
            public static int Status;
            public decimal this[dynamic x]
            {
                get
                {
                    MyClass.Status = 1;
                    return 1m;
                }

                set
                {
                    //We look at the value element to make sure it got passed in correctly
                    if (value == decimal.One)
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
            decimal d = mc[null];
            if (d != 1m || MyClass.Status != 1)
                return 1;
            mc[3] = decimal.One;
            if (MyClass.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}
