// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.integeregererface01.integeregererface01
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Types Rule : Interface Methods (Compiler)
    //          CLS-compliant language compilers must have syntax for the situation where a single type implements
    //          two interfaces and each of those interfaces requires the definition of a method with the same name and signature.
    //          Such methods must be considered distinct and need not have the same implementation.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    namespace MyNamespace
    {
        public interface MyInterface1
        {
            dynamic Method01(int n);
            T Method02<T>(dynamic n);
            dynamic Method03(byte b, ref dynamic n);
        }

        public interface MyInterface2
        {
            dynamic Method01(int n);
            T Method02<T>(dynamic n);
            dynamic Method03(byte b, ref dynamic n);
        }

        public class MyClass : MyInterface1, MyInterface2
        {
            dynamic MyInterface1.Method01(int n)
            {
                return default(dynamic);
            }

            T MyInterface1.Method02<T>(dynamic n)
            {
                return default(T);
            }

            dynamic MyInterface1.Method03(byte b, ref dynamic n)
            {
                return default(dynamic);
            }

            dynamic MyInterface2.Method01(int n)
            {
                return default(dynamic);
            }

            T MyInterface2.Method02<T>(dynamic n)
            {
                return default(T);
            }

            dynamic MyInterface2.Method03(byte b, ref dynamic n)
            {
                return default(dynamic);
            }
        }

        public struct MyStruct : MyInterface1, MyInterface2
        {
            public dynamic Method01(int n)
            {
                return default(dynamic);
            }

            public T Method02<T>(dynamic n)
            {
                return default(T);
            }

            public dynamic Method03(byte b, ref dynamic n)
            {
                return default(dynamic);
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.mixedmode02.mixedmode02
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Naming Rule : Characters and casing
    //          CLS-compliant language compilers must follow the rules of Annex 7 of Technical Report 15 of
    //          the Unicode Standard 3.0, which governs the set of characters that can start and be included in identifiers.
    //          This standard is available from the Web site of the Unicode Consortium.
    //          For two identifiers to be considered distinct, they must differ by more than just their case.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(79,55\).*CS3022</Expects>
    //<Expects Status=warning>\(80,55\).*CS3022</Expects>
    //<Expects Status=warning>\(47,29\).*CS3026.*vd1</Expects>
    //<Expects Status=warning>\(50,23\).*CS3002.*RefMethod01\<T\>\(ref dynamic, T\)</Expects>
    //<Expects Status=warning>\(51,23\).*CS3002.*refMethod01\<T\></Expects>
    //<Expects Status=warning>\(51,23\).*CS3005</Expects>
    //<Expects Status=warning>\(65,13\).*CS3010.*MyInterface\<T,U,V\>.Method01</Expects>
    //<Expects Status=warning>\(69,7\).*CS3005.*MyInterface\<T,U,V\>.method02</Expects>
    //<Expects Status=warning>\(72,13\).*CS3005.*method03\<X,Y\></Expects>
    //<Expects Status=warning>\(75,27\).*CS3010.*MyInterface\<T,U,V\>.MyEvent</Expects>
    //<Expects Status=warning>\(75,27\).*CS3010</Expects>
    //<Expects Status=warning>\(75,27\).*CS3010</Expects>
    //<Expects Status=warning>\(80,22\).*CS3005.*myDelegate01\<T\></Expects>
    //<Expects Status=success></Expects>
    // <Code>
    // <Expects Status=notin>CS3005.*outMethod01\<X\></Expects>
    // <Expects Status=notin>CS3005.*method01\<X\></Expects>
    // <Expects Status=notin>CS3005.*myDelegate02\<U,V\></Expects>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    [type: System.CLSCompliant(false)]
    public class MyClass<T>
    {
        public volatile dynamic vd;
        public static T OutMethod01<X>(T t, ref X x, out dynamic d)
        {
            d = default(object);
            return default(T);
        }

        public static T outMethod01<X>(T t, ref X x, out dynamic d)
        {
            d = default(object);
            return default(T);
        }
    }

    [type: System.CLSCompliant(true)]
    public struct MyStruct<U, V>
    {
        public volatile dynamic vd1;
        [method: System.CLSCompliant(true)]
        public MyClass<T> RefMethod01<T>(ref dynamic d, T t)
        {
            d = default(object);
            return new MyClass<T>();
        }

        public MyClass<T> refMethod01<T>(ref dynamic d, T t)
        {
            d = default(object);
            return new MyClass<T>();
        }

        [property: System.CLSCompliant(false)]
        public dynamic Prop
        {
            get;
            set;
        }

        public dynamic prop
        {
            get;
            set;
        }

        [field: System.CLSCompliant(false)]
        public dynamic field;
        public dynamic fielD;
    }

    public interface MyInterface<T, U, V>
    {
        [method: System.CLSCompliant(false)]
        dynamic Method01<X>(X n);
        dynamic method01<X>(X n);
        T Method02(dynamic n, U u);
        T method02(dynamic n, U u);
        [method: System.CLSCompliant(true)]
        dynamic Method03<X, Y>(out X x, ref Y y, dynamic n);
        dynamic method03<X, Y>(out X x, ref Y y, dynamic n);
        [event: System.CLSCompliant(false)]
        event MyDelegate01<T> MyEvent;
        event MyDelegate01<T> Myevent;
    }

    public delegate void MyDelegate01<T>(ref T t, [param: System.CLSCompliant(false)]
    dynamic d, int n);
    public delegate void myDelegate01<T>(ref T t, [param: System.CLSCompliant(true)]
    dynamic d, int n);
    [System.CLSCompliantAttribute(false)]
    public delegate V MyDelegate02<U, V>(U u, params dynamic[] ary);
    public delegate V myDelegate02<U, V>(U u, params dynamic[] ary);
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.namingchr02.namingchr02
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Naming Rule : Characters and casing
    //          CLS-compliant language compilers must follow the rules of Annex 7 of Technical Report 15 of
    //          the Unicode Standard 3.0, which governs the set of characters that can start and be included in identifiers.
    //          This standard is available from the Web site of the Unicode Consortium.
    //          For two identifiers to be considered distinct, they must differ by more than just their case.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(30,14\).*CS3005.*outMethod01\<X\></Expects>
    //<Expects Status=warning>\(36,23\).*CS3005.*refMethod01\<T\></Expects>
    //<Expects Status=warning>\(42,13\).*CS3005.*method01\<X\></Expects>
    //<Expects Status=warning>\(45,7\).*CS3005.*MyInterface\<T,U,V\>.method02</Expects>
    //<Expects Status=warning>\(48,13\).*CS3005.*method03\<X,Y\></Expects>
    //<Expects Status=warning>\(52,22\).*CS3005.*myDelegate01\<T\></Expects>
    //<Expects Status=warning>\(55,19\).*CS3005.*myDelegate02\<U,V\></Expects>
    //<Expects Status=success></Expects>
    // <Code>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    public class MyClass<T>
    {
        public T OutMethod01<X>(T t, ref X x, out dynamic d)
        {
            d = default(object);
            return default(T);
        }

        public T outMethod01<X>(T t, ref X x, out dynamic d)
        {
            d = default(object);
            return default(T);
        }
    }

    public struct MyStruct<U, V>
    {
        public MyClass<T> RefMethod01<T>(ref dynamic d, T t)
        {
            d = default(object);
            return new MyClass<T>();
        }

        public MyClass<T> refMethod01<T>(ref dynamic d, T t)
        {
            d = default(object);
            return new MyClass<T>();
        }
    }

    public interface MyInterface<T, U, V>
    {
        dynamic Method01<X>(X n);
        dynamic method01<X>(X n);
        T Method02(dynamic n, U u);
        T method02(dynamic n, U u);
        dynamic Method03<X, Y>(out X x, ref Y y, dynamic n);
        dynamic method03<X, Y>(out X x, ref Y y, dynamic n);
    }

    public delegate void MyDelegate01<T>(ref T t, dynamic d, int n);
    public delegate void myDelegate01<T>(ref T t, dynamic d, int n);
    public delegate V MyDelegate02<U, V>(U u, params dynamic[] ary);
    public delegate V myDelegate02<U, V>(U u, params dynamic[] ary);
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.namingchr03.namingchr03
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Naming Rule : Characters and casing
    //          CLS-compliant language compilers must follow the rules of Annex 7 of Technical Report 15 of
    //          the Unicode Standard 3.0, which governs the set of characters that can start and be included in identifiers.
    //          This standard is available from the Web site of the Unicode Consortium.
    //          For two identifiers to be considered distinct, they must differ by more than just their case.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(57,24\).*CS0108</Expects>
    //<Expects Status=warning>\(41,24\).*CS3005.*method01</Expects>
    //<Expects Status=warning>\(42,18\).*CS3005.*method02\<T\></Expects>
    //<Expects Status=warning>\(49,24\).*CS3005.*classIdentifier</Expects>
    //<Expects Status=warning>\(50,24\).*CS3005.*MEthod01</Expects>
    //<Expects Status=warning>\(51,18\).*CS3005.*mEthod02\<T\></Expects>
    //<Expects Status=warning>\(52,24\).*CS3005.*method03\<X,Y></Expects>
    //<Expects Status=warning>\(57,24\).*CS3005</Expects>
    //<Expects Status=warning>\(58,24\).*CS3005.*MyClass3\<U,V\>\.Method01</Expects>
    //<Expects Status=warning>\(59,24\).*CS3005.*MyClass3\<U,V\>\.Method03</Expects>
    //<Expects Status=success></Expects>
    // <Code>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    namespace MyNamespace
    {
        public class MyBase
        {
            public dynamic Method01(int n, ref dynamic d)
            {
                return default(object);
            }

            public T Method02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }
        }

        public class MyClass : MyBase
        {
            public dynamic ClassIdentifier;
            public dynamic method01(int n, ref dynamic d)
            {
                return default(object);
            }

            public T method02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }

            public dynamic Method03<X, Y>(X x, ref Y y, params dynamic[] ary)
            {
                return default(object);
            }
        }

        public class MyClass2 : MyClass
        {
            public dynamic classIdentifier;
            public dynamic MEthod01(int n, ref dynamic d)
            {
                return default(object);
            }

            public T mEthod02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }

            public dynamic method03<X, Y>(X x, ref Y y, params dynamic[] ary)
            {
                return default(object);
            }
        }

        public class MyClass3<U, V> : MyClass2
        {
            public dynamic ClassIdentifier;
            public dynamic Method01(long n, ref dynamic d)
            {
                return default(object);
            }

            public dynamic Method03(U x, ref V y, params dynamic[] ary)
            {
                return default(object);
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.namingchr04.namingchr04
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Visibility - CLS rules apply only to those parts of a type that are exposed outside the defining assembly.
    //      Naming Rule : Characters and casing
    //          CLS-compliant language compilers must follow the rules of Annex 7 of Technical Report 15 of
    //          the Unicode Standard 3.0, which governs the set of characters that can start and be included in identifiers.
    //          This standard is available from the Web site of the Unicode Consortium.
    //          For two identifiers to be considered distinct, they must differ by more than just their case.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(31,17\).*CS0169</Expects>
    //<Expects Status=warning>\(43,25\).*CS0169</Expects>
    //<Expects Status=success></Expects>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    namespace MyNamespace
    {
        public class MyBase
        {
            public dynamic[][][] array; //jagged array
            private dynamic Method01(int n, ref dynamic d)
            {
                return default(object);
            }

            private T Method02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }
        }

        public class MyClass : MyBase
        {
            private dynamic _classIdentifier;
            internal dynamic method01(int n, ref dynamic d)
            {
                return default(object);
            }

            protected T method02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }

            private dynamic Method03<X, Y>(X x, ref Y y, params dynamic[] ary)
            {
                return default(object);
            }
        }

        public class MyClass2 : MyClass
        {
            //public static dynamic[,,,] array1; //cube array
            private dynamic _classIdentifier;
            private dynamic MEthod01(int n, ref dynamic d)
            {
                return default(object);
            }

            private T mEthod02<T>(T t, out dynamic d)
            {
                d = default(object);
                return default(T);
            }

            protected dynamic method03<X, Y>(X x, ref Y y, params dynamic[] ary)
            {
                return default(object);
            }
        }

        public class MyClass3<U, V> : MyClass2
        {
            protected dynamic ClassIdentifier;
            private dynamic Method01(long n, ref dynamic d)
            {
                return default(object);
            }

            protected internal dynamic method03(U x, ref V y, params dynamic[] ary)
            {
                return default(object);
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.namingkeyword01.namingkeyword01
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Naming Rule : Keywords (Compiler)
    //          CLS-compliant language compilers supply a mechanism for referencing identifiers that coincide with keywords.
    //          CLS-compliant language compilers provide a mechanism for defining and overriding virtual methods
    //          with names that are keywords in the language.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //[assembly: System.CLSCompliant(true)]
    namespace MyNamespace
    {
        public class MyClass
        {
            public dynamic @dynamic;
            public dynamic Method01(ref int @dynamic)
            {
                return default(dynamic);
            }
        }

        public struct MyStruct
        {
            public dynamic @dynamic;
            public void Method02(out dynamic @dynamic)
            {
                @dynamic = default(dynamic);
            }
        }

        public interface MyInterface
        {
            dynamic Method01(int n, dynamic @dynamic);
            dynamic @dynamic(string n, dynamic d);
        }

        public delegate void myDelegate02(params dynamic[] @dynamic);
        public delegate int @dynamic(int n);
        namespace MyNamespace11
        {
            public enum @dynamic
            {
            }
        }
    }

    namespace MyNamespace1
    {
        public class @dynamic
        {
            private dynamic Method01(out int n, dynamic d)
            {
                n = 0;
                return default(dynamic);
            }
        }

        namespace MyNamespace2
        {
            public struct @dynamic
            {
                private dynamic Method01(int n, ref dynamic d)
                {
                    return default(dynamic);
                }
            }

            namespace MyNamespace3
            {
                public interface @dynamic
                {
                    dynamic Method01(int n, ref dynamic d);
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.typegeneral01.typegeneral01
{
    // <Title> CLS Compliance for Dynamic </Title>
    // <Description> Types Rule : Interface Methods (Compiler)
    //          CLS-compliant language compilers must have syntax for the situation where a single type implements
    //          two interfaces and each of those interfaces requires the definition of a method with the same name and signature.
    //          Such methods must be considered distinct and need not have the same implementation.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    //[assembly: System.CLSCompliant(true)]
    namespace MyNamespace
    {
        public class MyClass
        {
            public dynamic Method01(int x, int y = 0, int z = 1)
            {
                return default(dynamic);
            }

            //
            public T Method02<T>(dynamic d = default(dynamic), string s = "QQQ")
            {
                return default(T);
            }

            
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                MyClass c = new MyClass();
                c.Method01(999);
                c.Method01(9, 8);
                c.Method01(9, 8, 7);
                c.Method01(999, z: 888);
                c.Method01(999, z: 888, y: 666);
                c.Method02<byte>();
                c.Method02<int>(default(dynamic));
                c.Method02<long>(default(dynamic), "CCC");
                var v = new MyStruct<dynamic>();
                v.Method11();
                v.Method11(default(dynamic));
                v.Method11(default(object), default(object), default(object));
                v.Method12<int, dynamic>();
                v.Method12<int, dynamic>(100);
                v.Method12<int, dynamic>(-9999, default(dynamic));
                return 0;
            }
        }

        public struct MyStruct<T>
        {
            public dynamic Method11(T t = default(T), params dynamic[] ary)
            {
                return default(dynamic);
            }

            public T Method12<U, V>(U u = default(U), V v = default(V))
            {
                return default(T);
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.ClsCompliance.bug89385_a.bug89385_a
{
    // <Title> regression test</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public static void Method(string s)
        {
            if (s == "abc")
                Test.Result++;
        }

        public string Prop
        {
            get;
            set;
        }
    }

    public class Test
    {
        public Foo Foo
        {
            get;
            set;
        }

        public static void DoExample(dynamic d)
        {
            Foo.Method(d.Prop);
        }

        public static int Result = -1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                DoExample(new Foo()
                {
                    Prop = "abc"
                }

                );
            }
            catch (System.Exception)
            {
                Test.Result--;
            }

            return Test.Result;
        }
    }
}
