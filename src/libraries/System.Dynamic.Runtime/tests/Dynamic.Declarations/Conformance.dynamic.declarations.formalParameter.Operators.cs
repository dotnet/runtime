// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Operators.binary001.binary001
{
    public class Test
    {
        public class MyClass
        {
            public int Field;
            public static MyClass operator +(dynamic d, MyClass x)
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
            dynamic mc = 3;
            MyClass x = new MyClass()
            {
                Field = 4
            }

            ;
            MyClass m = mc + x;
            if (m.Field == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Operators.binary002.binary002
{
    public class Test
    {
        public class MyClass
        {
            public int Field;
            public static MyClass operator &(MyClass x, dynamic d)
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
            dynamic mc = 3;
            MyClass x = new MyClass();
            MyClass m = x & mc;
            if (m.Field == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Operators.binary003.binary003
{
    public class Test
    {
        public class MyClass
        {
            public static bool operator ==(MyClass x, dynamic d)
            {
                return true;
            }

            public static bool operator !=(MyClass x, dynamic d)
            {
                return false;
            }

            public override bool Equals(object o)
            {
                return this == o;
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }

        public class MyClass02
        {
            public static bool operator ==(MyClass02 x, object d)
            {
                return true;
            }

            public static bool operator !=(MyClass02 x, object d)
            {
                return false;
            }

            public override bool Equals(object o)
            {
                return this == o;
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic mc = 3;
            var v = new MyClass02();
            bool rez = v == mc;
            rez &= !(v != mc);
            MyClass x = new MyClass();
            rez &= x == mc;
            rez &= !(x != mc);
            return rez ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Operators.binary004.binary004
{
    public class Test
    {
        public class MyClass
        {
            public static bool operator <=(MyClass x, dynamic d)
            {
                return true;
            }

            public static bool operator >=(MyClass x, dynamic d)
            {
                return false;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic mc = 3;
            MyClass x = new MyClass();
            bool rez = x <= mc;
            if (rez == false)
                return 1;
            rez = x >= mc;
            if (rez == true)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.formalParameter.Operators.binary005.binary005
{
    public class Test
    {
        public class MyClass
        {
            public static bool operator ^(dynamic d, MyClass x)
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
            dynamic mc = 3;
            MyClass x = new MyClass();
            bool rez = mc ^ x;
            if (rez == true)
                return 0;
            return 1;
        }
    }
    // </Code>
}
