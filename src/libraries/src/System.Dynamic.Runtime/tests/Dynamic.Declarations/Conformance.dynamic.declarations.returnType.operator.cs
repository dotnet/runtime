// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary001.binary001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary001.binary001;

    public class Test
    {
        private class MyClass
        {
            public static dynamic operator &(MyClass x, int d)
            {
                return true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass x = new MyClass();
            dynamic rez = x & 3;
            if ((bool)rez == true)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary002.binary002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary002.binary002;

    public class Test
    {
        public class MyClass
        {
            public static dynamic operator /(dynamic d, MyClass x)
            {
                return int.MinValue;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic x = new MyClass();
            dynamic rez = x / new MyClass();
            if ((int)rez == int.MinValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary003.binary003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary003.binary003;

    public class Test
    {
        public class MyClass
        {
            public static dynamic operator ^(MyClass x, dynamic d)
            {
                return int.MinValue;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic x = new MyClass();
            dynamic rez = new MyClass() ^ x;
            if ((int)rez == int.MinValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary004.binary004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.binary004.binary004;

    public class Test
    {
        private class MyClass
        {
            public static dynamic operator >>(MyClass x, int d)
            {
                return true;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass x = new MyClass();
            dynamic rez = x >> 3;
            if ((bool)rez == true)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.unary002.unary002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.operate.unary002.unary002;

    public class Test
    {
        public class MyClass
        {
            public int Field;
            public static dynamic operator -(MyClass d)
            {
                return new MyClass()
                {
                    Field = 3
                }

                ;
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
            dynamic m = -(MyClass)mc;
            if ((int)m.Field == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}