// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property001.property001
{
    public class Test
    {
        private class MyClass
        {
            public dynamic Foo
            {
                get
                {
                    dynamic d = new object();
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
            dynamic d = mc.Foo;
            if (d != null)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property002.property002
{
    public class Test
    {
        private class MyClass
        {
            public static dynamic Foo
            {
                get
                {
                    dynamic d = new object();
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
            dynamic d = MyClass.Foo;
            if (d != null)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property003.property003
{
    public class Test
    {
        private class MyClass<T>
        {
            public static dynamic Foo
            {
                get
                {
                    dynamic d = default(T);
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
            dynamic d = MyClass<float>.Foo;
            if ((float)d != 0f)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property004.property004
{
    public class Test
    {
        private class MyClass<T>
        {
            public dynamic Foo
            {
                get
                {
                    dynamic d = default(T);
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
            MyClass<decimal> mc = new MyClass<decimal>();
            dynamic d = mc.Foo;
            if ((decimal)d == 0m)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property005.property005
{
    public class Test
    {
        private class MyClass<T>
        {
            public int x;
            public dynamic Foo
            {
                set
                {
                    x = (int)value;
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
            MyClass<decimal> mc = new MyClass<decimal>();
            mc.Foo = 3;
            if (mc.x != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property006.property006
{
    public class Test
    {
        private class MyClass<T>
        {
            public static float f;
            public static dynamic Foo
            {
                get
                {
                    return f;
                }

                set
                {
                    f = (float)value;
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
            dynamic d = 3f;
            MyClass<float>.Foo = d;
            if (MyClass<float>.f != 3f)
                return 1;
            d = MyClass<float>.Foo;
            if ((float)d != 3f)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property007.property007
{
    public class Test
    {
        private class MyClass
        {
            public decimal dec;
            public dynamic Foo
            {
                get
                {
                    return dec;
                }

                set
                {
                    dec = value;
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
            dynamic d = 3m;
            mc.Foo = d;
            if (mc.dec != 3m)
                return 1;
            d = mc.Foo;
            if ((decimal)d != 3m)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.returnType.properties.property008.property008
{
    public class Test
    {
        private class MyClass
        {
            public static string s;
            public static dynamic Foo
            {
                get
                {
                    return s;
                }

                set
                {
                    s = value;
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
            dynamic dd = "string";
            MyClass.Foo = dd;
            if (MyClass.s != "string")
                return 1;
            dynamic d = MyClass.Foo;
            if (d != "string")
                return 1;
            return 0;
        }
    }
    // </Code>
}
