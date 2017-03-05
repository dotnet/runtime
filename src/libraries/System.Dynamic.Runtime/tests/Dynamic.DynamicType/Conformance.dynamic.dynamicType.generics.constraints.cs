// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex001.complex001
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass<T>
        where T : List<object>
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<List<object>> mc = new MyClass<List<dynamic>>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex002.complex002
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass<T>
        where T : List<object>
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<List<dynamic>> mc = new MyClass<List<dynamic>>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex008.complex008
{
    // <Title>Generic constraints</Title>
    // <Description> Trying to pass in int and dynamic as type parameters used to give an error saying that there is no boxing conversion from int to dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class M
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = M1<int, dynamic>();
            rez += M2<int, dynamic>();
            int i = 4;
            dynamic d = 4;
            // Simple call let Runtime decide d's type which is 'int' instead of 'object'
            rez += M3(i, d);
            return rez > 0 ? 1 : 0;
        }

        public static int M3<T, S>(T t, S s) where T : S
        {
            // if (typeof(T) != typeof(int) || typeof(S) != typeof(object)) return 1;
            if (typeof(T) != typeof(int) || typeof(S) != typeof(int))
                return 1;
            return 0;
        }

        public static int M2<T, S>() where T : struct, S
        {
            if (typeof(T) != typeof(int) || typeof(S) != typeof(object))
                return 1;
            return 0;
        }

        public static int M1<T, S>() where T : S
        {
            if (typeof(T) != typeof(int) || typeof(S) != typeof(object))
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex009.complex009
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            c.NakedGen1<GenDerClass<int>, GenBaseClass<int>>();
            c.NakedGen1<GenDerClass<Struct>, GenBaseClass<Struct>>();
            return 0;
        }
    }

    public class C
    {
        public void NakedGen1<T, U>() where T : U
        {
        }
    }

    public struct Struct
    {
    }

    public class GenBaseClass<T>
    {
    }

    public class GenDerClass<T> : GenBaseClass<T>
    {
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex010.complex010
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public virtual void Foo<G>() where G : T, new()
        {
        }
    }

    public class DerivedNullableOfInt : Base<int?>
    {
        public override void Foo<G>()
        {
            dynamic d = new G();
            d = 4;
            d.ToString();
            Program.Status = 1;
        }
    }

    public class Program
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new DerivedNullableOfInt();
            d.Foo<int?>();
            if (Program.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex011.complex011
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Base<T>
    {
        public virtual IEnumerable<G> Foo<G>() where G : T, new()
        {
            return null;
        }
    }

    public class DerivedNullableOfInt : Base<int?>
    {
        public override IEnumerable<G> Foo<G>()
        {
            yield return new G();
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new DerivedNullableOfInt();
            var x = d.Foo<int?>();
            return x != null ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.complex012.complex012
{
    // <Title>Derived generic types</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Base<T>
    {
        public virtual int Foo<G>() where G : T, new()
        {
            return -1;
        }
    }

    public class DerivedNullableOfInt : Base<int?>
    {
        public override int Foo<G>()
        {
            int i = 3;
            Func<G, int> f = x => i;
            return f(new G());
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new DerivedNullableOfInt();
            var x = d.Foo<int?>();
            if (x != null)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple001.simple001
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
        where T : class
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<object> mc = new MyClass<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple002.simple002
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
        where T : class
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<dynamic> mc = new MyClass<object>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple006.simple006
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<object> mc = new MyClass<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple008.simple008
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T, U>
        where T : U
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<dynamic, object> mc = new MyClass<dynamic, object>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple009.simple009
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
        where T : class
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<dynamic> mc = new MyClass<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple012.simple012
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class MyClass<T, U>
        where T : U
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<object, object> mc = new MyClass<dynamic, dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple013.simple013
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T, U>
        where T : U
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<dynamic, object> mc = new MyClass<object, dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple015.simple015
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
        where T : new()
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<dynamic> mc = new MyClass<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple016.simple016
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass<T>
        where T : new()
    {
        public void Foo()
        {
            Test.Status = 1;
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
            MyClass<object> mc = new MyClass<dynamic>();
            mc.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple018.simple018
{
    // <Title>Generic constraints</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new Test();
            x.Bar<string, dynamic>();
            var y = new Test();
            y.Bar<string, dynamic>(); // used to be error CS0311:
            // The type 'string' cannot be used as type parameter 'T' in the generic type or method 'A.Foo<T,S>()'.
            // There is no implicit reference conversion from 'string' to '::dynamic'.
            return 0;
        }

        public void Bar<T, S>() where T : S
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple019.simple019
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class B
    {
        public static int Status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new B();
            x.Foo<int>();
            return B.Status;
        }

        public void Foo<T, S>() where T : S
        {
            B.Status = 1;
        }

        public void Foo<T>()
        {
            B.Status = 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple020.simple020
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class B
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new B();
            try
            {
                x.Foo(); // Unexpected NullReferenceException
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "B.Foo<T>()"))
                    return 0;
            }

            return 1;
        }

        public void Foo<T>()
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple021.simple021
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class B
    {
        public static int Status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new B();
            x.Foo<int, int>();
            return B.Status;
        }

        public void Foo<T, S>() where T : S // The constraint is important part
        {
            B.Status = 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.simple022.simple022
{
    // <Title>Generic constraints</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class B
    {
        public static int Status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new B();
            try
            {
                x.Foo<int>(); // Unexpected NullReferenceException
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArity, e.Message, "B.Foo<T,S>()", ErrorVerifier.GetErrorElement(ErrorElementId.SK_METHOD), "2"))
                    B.Status = 0;
            }

            return B.Status;
        }

        public void Foo<T, S>() where T : S
        {
            B.Status = 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference001.typeinference001
{
    // <Title>Generic Type Inference</Title>
    // <Description> Runtime type inference succeeds in cases where it should fail
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class C
    {
        public static void M<T>(T x, T y)
        {
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
            string x = "string";
            dynamic y = 7;
            try
            {
                C.M(x, y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "C.M<T>(T, T)"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference002.typeinference002
{
    // <Title>Generic Type Inference</Title>
    // <Description> 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public void M<T>(T x, T y)
        {
        }
    }

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public struct Test
    {
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var vv = new A();
            dynamic vd = new A();
            int ret = 0;
            // dyn (null) & string -> string
            dynamic dynPara = null;
            string str = "QC";
            try
            {
                vv.M(dynPara, str);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("1)" + ex);
                ret++; // Fail
            }

            try
            {
                vd.M(dynPara, str);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("2)" + ex);
                ret++; // Fail
            }

            // dyn (null) & class
            dynPara = null;
            var a = new A();
            try
            {
                vv.M(dynPara, a);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("3)" + ex);
                ret++; // Fail
            }

            try
            {
                vd.M(a, dynPara);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("4)" + ex);
                ret++; // Fail
            }

            // dyn (class), dyn (null)
            dynPara = new A();
            dynamic dynP2 = null;
            try
            {
                vv.M(dynPara, dynP2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("5)" + ex);
                ret++; // Fail
            }

            try
            {
                vd.M(dynP2, dynPara);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("6)" + ex);
                ret++; // Fail
            }

            return 0 == ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference003.typeinference003
{
    // <Title>Generic Type Inference</Title>
    // <Description> 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public struct S
    {
        public void M<T>(T x, T y)
        {
        }
    }

    public struct Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var vv = new S();
            dynamic vd = new S();
            int ret = 6;
            // dyn (int), string (null)
            dynamic dynPara = 100;
            string str = null;
            try
            {
                vv.M(str, dynPara);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "S.M<T>(T, T)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(dynPara, str);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "S.M<T>(T, T)"))
                    ret--; // Pass
            }

            // dyn (null), int -\-> int?
            dynPara = null;
            int n = 0;
            try
            {
                vv.M(dynPara, n);
                System.Console.WriteLine("3) no ex");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "S.M<int>(int, int)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(n, dynPara);
                System.Console.WriteLine("4) no ex");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "S.M<int>(int, int)"))
                    ret--; // Pass
            }

            // dyn->struct, dyn->null
            dynPara = new Test();
            dynamic dynP2 = null;
            try
            {
                vv.M(dynPara, dynP2);
                System.Console.WriteLine("5) no ex");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "S.M<Test>(Test, Test)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(dynPara, dynP2);
                System.Console.WriteLine("6) no ex");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "S.M<Test>(Test, Test)"))
                    ret--; // Pass
            }

            return 0 == ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference004.typeinference004
{
    // <Title>Generic Type Inference</Title>
    // <Description> 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public void M<T>(T x, T y, T z)
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var obj = new Test();
            dynamic dobj = new Test();
            int ret = 0;
            // -> object
            dynamic d = 11;
            string str = "string";
            object o = null;
            try
            {
                obj.M(d, str, o);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("1)" + ex);
                ret++; // Fail
            }

            try
            {
                dobj.M(o, d, str);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("2)" + ex);
                ret++; // Fail
            }

            // -> int?
            d = null;
            int n1 = 111111;
            int? n2 = 11;
            try
            {
                obj.M(n1, d, n2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("3)" + ex);
                ret++; // Fail
            }

            try
            {
                dobj.M(n1, n2, d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("4)" + ex);
                ret++; // Fail
            }

            // -> long
            d = 0;
            var v = -50000000000; // long
            byte b = 1;
            try
            {
                obj.M(d, v, b);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("5)" + ex);
                ret++; // Fail
            }

            try
            {
                dobj.M(b, d, v);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("6)" + ex);
                ret++; // Fail
            }

            // ->
            d = null;
            dynamic d2 = 0;
            long? sb = null; // failed for (s)byte, (u)short, etc.
            try
            {
                obj.M<long?>(d, sb, d2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("7)" + ex);
                ret++; // Fail
            }

            try
            {
                dobj.M<long?>(sb, d2, d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex) // Should Not Ex
            {
                System.Console.WriteLine("8)" + ex);
                ret++; // Fail
            }

            return 0 == ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference005.typeinference005
{
    // <Title>Generic Type Inference</Title>
    // <Description> 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public struct Test
    {
        public void M<T>(T x, T y, T z)
        {
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var vv = new Test();
            dynamic vd = new Test();
            int ret = 6;
            string x = "string";
            int? y = null;
            dynamic z = 7;
            try
            {
                vv.M(x, y, z);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "Test.M<T>(T, T, T)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(x, z, y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "Test.M<T>(T, T, T)"))
                    ret--; // Pass
            }

            Test? o1 = null;
            char ch = '\0';
            z = "";
            try
            {
                vv.M(z, o1, ch);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "Test.M<T>(T, T, T)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(ch, z, o1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "Test.M<T>(T, T, T)"))
                    ret--; // Pass
            }

            dynamic z2 = 100;
            z = null;
            try
            {
                vv.M(z2, ch, z);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Test.M<int>(int, int, int)"))
                    ret--; // Pass
            }

            try
            {
                vd.M(z, z2, ch);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Test.M<int>(int, int, int)"))
                    ret--; // Pass
            }

            return 0 == ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.cnstraintegeregers.typeinference006.typeinference006
{
    // <Title>Generic Type Inference</Title>
    // <Description> We used to give compiler errors in these cases
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections.Generic;

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            dynamic x = 1;
            rez += Bar1(1, x);
            rez += Bar1((dynamic)1, x);
            dynamic d = new List<List<int>>();
            rez += Bar2(d, 1);
            rez += Bar2(d, (dynamic)1);
            var l = new List<List<int>>();
            rez += Bar2(l, x);
            rez += Bar3(1, x, x);
            rez += Bar3(1, 1, x);
            var cls = new C<int>();
            rez += cls.Foo(1, x);
            rez += cls.Foo(x, 1);
            rez += Bar4(x, 1);
            rez += Bar4(1, x);
            return rez;
        }

        public static int Bar1<T, S>(T x, S y) where T : IComparable<S>
        {
            return 0;
        }

        public static int Bar2<T, S>(T t, S s) where T : IList<List<S>>
        {
            return 0;
        }

        public static int Bar3<T, U, V>(T t, U u, V v) where T : U where U : IComparable<V>
        {
            return 0;
        }

        public static int Bar4<T, U>(T t, IComparable<U> u) where T : IComparable<U>
        {
            return 0;
        }
    }

    public class C<T>
    {
        public int Foo<U>(T t, U u) where U : IComparable<T>
        {
            return 0;
        }
    }
    // </Code>
}
