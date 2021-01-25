// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested001.nested001
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<dynamic>.Derived<dynamic> d = new Base<dynamic>.Derived<dynamic>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested002.nested002
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<object>.Derived<dynamic> d = new Base<object>.Derived<dynamic>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested003.nested003
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<dynamic>.Derived<dynamic> d = new Base<object>.Derived<dynamic>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested004.nested004
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<object>.Derived<object> d = new Base<dynamic>.Derived<dynamic>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested005.nested005
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<object>.Derived<dynamic> d = new Base<object>.Derived<object>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested006.nested006
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<object>.Derived<dynamic> d = new Base<object>.Derived<object>();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nested007.nested007
{
    // <Title>Generic constraints for nested types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Foo
    {
    }

    public class Base<T>
        where T : Foo
    {
        public class Derived<U>
            where U : T
        {
            public void Foo()
            {
                Test.Status = 1;
            }
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base<Foo>.Derived<Foo> d = new Base<Foo>.Derived<Foo>();
            d.Foo();
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass001.nestedclass001
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(16,22\).*CS0693</Expects>
    //<Expects Status=warning>\(19,20\).*CS0649</Expects>

    public class A<T>
    {
        public class Gen<T>
        {
            public int x;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj = new A<int>.Gen<int>();
            Obj.x = 10;
            if (Obj.x != 10)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass002.nestedclass002
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(18,21\).*CS0693</Expects>
    //<Expects Status=warning>\(23,25\).*CS0693</Expects>
    //<Expects Status=warning>\(21,20\).*CS0649</Expects>
    //<Expects Status=warning>\(25,24\).*CS0649</Expects>

    public class A1<T, U>
    {
        public class A2<T>
        {
            public int x;
            public class A3<U>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float>();
            dynamic Obj2 = new A1<int, double>.A2<float>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass003.nestedclass003
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(22,25\).*CS0693</Expects>
    //<Expects Status=warning>\(20,20\).*CS0649</Expects>
    //<Expects Status=warning>\(24,24\).*CS0649</Expects>

    public class A1<T, U>
    {
        public class A2<V>
        {
            public int x;
            public class A3<V>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float>();
            dynamic Obj2 = new A1<int, double>.A2<float>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass004.nestedclass004
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(24,25\).*CS0693</Expects>
    //<Expects Status=warning>\(28,29\).*CS0693</Expects>
    //<Expects Status=warning>\(22,20\).*CS0649</Expects>
    //<Expects Status=warning>\(26,24\).*CS0649</Expects>
    //<Expects Status=warning>\(30,28\).*CS0649</Expects>

    public class A1<T>
    {
        public class A2<U>
        {
            public int x;
            public class A3<T>
            {
                public int x;
                public class A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int>.A2<float>();
            dynamic Obj2 = new A1<int>.A2<float>.A3<string>();
            dynamic Obj3 = new A1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            Obj1.x = 10;
            Obj2.x = 45;
            Obj3.x = 99;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            if (Obj3.x != 99)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass005.nestedclass005
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(29,25\).*CS0693</Expects>
    //<Expects Status=warning>\(33,29\).*CS0693</Expects>
    //<Expects Status=warning>\(51,25\).*CS0693</Expects>
    //<Expects Status=warning>\(55,29\).*CS0693</Expects>
    //<Expects Status=warning>\(27,20\).*CS0649</Expects>
    //<Expects Status=warning>\(31,24\).*CS0649</Expects>
    //<Expects Status=warning>\(35,28\).*CS0649</Expects>
    //<Expects Status=warning>\(49,20\).*CS0649</Expects>
    //<Expects Status=warning>\(53,24\).*CS0649</Expects>
    //<Expects Status=warning>\(57,28\).*CS0649</Expects>

    public class A1<T>
    {
        public class A2<U>
        {
            public int x;
            public class A3<T>
            {
                public int x;
                public class A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public class B1<T>
    {
        public class A2<U>
        {
            public int x;
            public class A3<T>
            {
                public int x;
                public class A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int>.A2<float>();
            dynamic Obj2 = new A1<int>.A2<float>.A3<string>();
            dynamic Obj3 = new A1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            dynamic Obj4 = new B1<int>.A2<float>();
            dynamic Obj5 = new B1<int>.A2<float>.A3<string>();
            dynamic Obj6 = new B1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            Obj1.x = 10;
            Obj2.x = 45;
            Obj3.x = 99;
            Obj4.x = -10;
            Obj5.x = -45;
            Obj6.x = -99;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            if (Obj3.x != 99)
                return 1;
            if (Obj4.x != -10)
                return 1;
            if (Obj5.x != -45)
                return 1;
            if (Obj6.x != -99)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass006.nestedclass006
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,22\).*CS0693</Expects>

    public class A<T>
    {
        public int Meth1<T>(int a)
        {
            return a;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj = new A<int>();
            if (Obj.Meth1<string>(10) != 10)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedclass007.nestedclass007
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(19,21\).*CS0693</Expects>
    //<Expects Status=warning>\(19,24\).*CS0693</Expects>
    //<Expects Status=warning>\(24,25\).*CS0693</Expects>
    //<Expects Status=warning>\(22,20\).*CS0649</Expects>
    //<Expects Status=warning>\(26,24\).*CS0649</Expects>

    public class A1<T, U>
    {
        public class A2<T, U>
        {
            public int x;
            public class A3<U>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float, decimal>();
            dynamic Obj2 = new A1<int, double>.A2<float, decimal>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct001.nestedstrct001
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(16,23\).*CS0693</Expects>
    //<Expects Status=warning>\(19,20\).*CS0649</Expects>

    public struct A<T>
    {
        public struct Gen<T>
        {
            public int x;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj = new A<int>.Gen<int>();
            Obj.x = 10;
            if (Obj.x != 10)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct002.nestedstrct002
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(18,22\).*CS0693</Expects>
    //<Expects Status=warning>\(23,26\).*CS0693</Expects>
    //<Expects Status=warning>\(21,20\).*CS0649</Expects>
    //<Expects Status=warning>\(25,24\).*CS0649</Expects>

    public struct A1<T, U>
    {
        public struct A2<T>
        {
            public int x;
            public struct A3<U>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float>();
            dynamic Obj2 = new A1<int, double>.A2<float>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct003.nestedstrct003
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(22,26\).*CS0693</Expects>
    //<Expects Status=warning>\(20,20\).*CS0649</Expects>
    //<Expects Status=warning>\(24,24\).*CS0649</Expects>

    public struct A1<T, U>
    {
        public struct A2<V>
        {
            public int x;
            public struct A3<V>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float>();
            dynamic Obj2 = new A1<int, double>.A2<float>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct004.nestedstrct004
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(24,26\).*CS0693</Expects>
    //<Expects Status=warning>\(28,30\).*CS0693</Expects>
    //<Expects Status=warning>\(22,20\).*CS0649</Expects>
    //<Expects Status=warning>\(26,24\).*CS0649</Expects>
    //<Expects Status=warning>\(30,28\).*CS0649</Expects>

    public struct A1<T>
    {
        public struct A2<U>
        {
            public int x;
            public struct A3<T>
            {
                public int x;
                public struct A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int>.A2<float>();
            dynamic Obj2 = new A1<int>.A2<float>.A3<string>();
            dynamic Obj3 = new A1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            Obj1.x = 10;
            Obj2.x = 45;
            Obj3.x = 99;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            if (Obj3.x != 99)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct005.nestedstrct005
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(29,26\).*CS0693</Expects>
    //<Expects Status=warning>\(33,30\).*CS0693</Expects>
    //<Expects Status=warning>\(51,26\).*CS0693</Expects>
    //<Expects Status=warning>\(55,30\).*CS0693</Expects>
    //<Expects Status=warning>\(27,20\).*CS0649</Expects>
    //<Expects Status=warning>\(31,24\).*CS0649</Expects>
    //<Expects Status=warning>\(35,28\).*CS0649</Expects>
    //<Expects Status=warning>\(49,20\).*CS0649</Expects>
    //<Expects Status=warning>\(53,24\).*CS0649</Expects>
    //<Expects Status=warning>\(57,28\).*CS0649</Expects>

    public struct A1<T>
    {
        public struct A2<U>
        {
            public int x;
            public struct A3<T>
            {
                public int x;
                public struct A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public struct B1<T>
    {
        public struct A2<U>
        {
            public int x;
            public struct A3<T>
            {
                public int x;
                public struct A4<U>
                {
                    public int x;
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int>.A2<float>();
            dynamic Obj2 = new A1<int>.A2<float>.A3<string>();
            dynamic Obj3 = new A1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            dynamic Obj4 = new B1<int>.A2<float>();
            dynamic Obj5 = new B1<int>.A2<float>.A3<string>();
            dynamic Obj6 = new B1<int>.A2<string>.A3<Test>.A4<A1<int>>();
            Obj1.x = 10;
            Obj2.x = 45;
            Obj3.x = 99;
            Obj4.x = -10;
            Obj5.x = -45;
            Obj6.x = -99;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            if (Obj3.x != 99)
                return 1;
            if (Obj4.x != -10)
                return 1;
            if (Obj5.x != -45)
                return 1;
            if (Obj6.x != -99)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct006.nestedstrct006
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,22\).*CS0693</Expects>

    public struct A<T>
    {
        public int Meth1<T>(int a)
        {
            return a;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj = new A<int>();
            if (Obj.Meth1<string>(10) != 10)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.nested.nestedstrct007.nestedstrct007
{
    // <Title>Generic nested types</Title>
    // <Description>
    //      Generic nested types with same type parameter names
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(19,22\).*CS0693</Expects>
    //<Expects Status=warning>\(19,25\).*CS0693</Expects>
    //<Expects Status=warning>\(24,26\).*CS0693</Expects>
    //<Expects Status=warning>\(22,20\).*CS0649</Expects>
    //<Expects Status=warning>\(26,24\).*CS0649</Expects>

    public struct A1<T, U>
    {
        public struct A2<T, U>
        {
            public int x;
            public struct A3<U>
            {
                public int x;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic Obj1 = new A1<int, double>.A2<float, decimal>();
            dynamic Obj2 = new A1<int, double>.A2<float, decimal>.A3<string>();
            Obj1.x = 10;
            Obj2.x = 45;
            if (Obj1.x != 10)
                return 1;
            if (Obj2.x != 45)
                return 1;
            return 0;
        }
    }
    // </Code>
}
