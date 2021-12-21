// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.Twondorder01.Twondorder01
{
    // <Area>variance</Area>
    // <Title> Higher order variance</Title>
    // <Description> 2nd order generic types </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public interface IMeta<out T>
    {
        int foo(IAction<T> a);
    }

    public class Meta<T> : IMeta<T> where T : new()
    {
        public int foo(IAction<T> a)
        {
            return a.boo(new T());
        }
    }

    public interface IAction<in T>
    {
        int boo(T t);
    }

    public class Action<T> : IAction<T>
    {
        public int boo(T t)
        {
            return 0;
        }
    }

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var v1 = new Action<Animal>();
            dynamic v2 = (IAction<Tiger>)v1;
            var x = v2.boo(new Tiger());
            dynamic m1 = new Meta<Tiger>();
            dynamic m2 = (IMeta<Animal>)m1;
            var y = m2.foo(v1);
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.Twondorder02.Twondorder02
{
    // <Area>variance</Area>
    // <Title> Higher order variance</Title>
    // <Description>declaration tests on interfaces -  2nd order generic types on return type, invariant => contravariant</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public delegate void Action<in T>(T t);
    public delegate void Meta<out T>(Action<T> action);
    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = (Action<Animal>)((Animal a) =>
            {
            }

            );
            dynamic v2 = v1;
            dynamic m1 = (Meta<Tiger>)((Action<Tiger> action) =>
            {
                action(new Tiger());
            }

            );
            dynamic m2 = (Meta<Animal>)m1;
            m2(v1);
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.multipleuse01.multipleuse01
{
    // <Area>variance</Area>
    // <Title> Using both Co and contravariance in types</Title>
    // <Description> basic contravariance on delegates </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class Fruit
    {
    }

    public class Apple : Fruit
    {
    }

    public class C
    {
        public delegate S Foo<in T, out S, in U>(T t, U u);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo<Animal, Apple, Fruit> f11 = (Animal a, Fruit fr) =>
            {
                return new Apple();
            }

            ;
            dynamic f12 = (Foo<Tiger, Fruit, Apple>)f11;
            var x1 = f12(new Tiger(), new Apple());
            dynamic f21 = (Foo<Animal, Apple, Fruit>)((Animal a, Fruit fr) =>
            {
                return new Apple();
            }

            );
            Foo<Tiger, Fruit, Apple> f22 = f21;
            var x2 = f22(new Tiger(), new Apple());
            dynamic f31 = (Foo<Animal, Apple, Fruit>)((Animal a, Fruit fr) =>
            {
                return new Apple();
            }

            );
            dynamic f32 = (Foo<Tiger, Fruit, Apple>)f31;
            var x3 = f32(new Tiger(), new Apple());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.multipleuse02.multipleuse02
{
    // <Area>variance</Area>
    // <Title> Using both Co and contravariance in types</Title>
    // <Description> variance on interfaces </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T, in U, in S>
    {
        T Boo();
    }

    public class Variance<T, U, S> : iVariance<T, U, S> where T : new()
    {
        public T Boo()
        {
            return new T();
        }
    }

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class Fruit
    {
    }

    public class Apple : Fruit
    {
    }

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v11 = new Variance<Tiger, Fruit, Fruit>();
            iVariance<Animal, Apple, Apple> v12 = v11;
            var x1 = v12.Boo();
            Variance<Tiger, Fruit, Fruit> v21 = new Variance<Tiger, Fruit, Fruit>();
            dynamic v22 = (iVariance<Animal, Apple, Apple>)v21;
            var x2 = v22.Boo();
            dynamic v31 = new Variance<Tiger, Fruit, Fruit>();
            dynamic v32 = (iVariance<Animal, Apple, Apple>)v31;
            var x3 = v32.Boo();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.multipleuse03.multipleuse03
{
    // <Area>variance</Area>
    // <Title> Using both Co and contravariance in types</Title>
    // <Description> variance on interfaces </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public interface iVariance<out T, in U, S>
    {
        T Boo();
    }

    public class Variance<T, U, S> : iVariance<T, U, S> where T : new()
    {
        public T Boo()
        {
            return new T();
        }
    }

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class Fruit
    {
    }

    public class Apple : Fruit
    {
    }

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            dynamic v11 = new Variance<Tiger, Fruit, Fruit>();
            try
            {
                result++;
                iVariance<Animal, Apple, Apple> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<Tiger,Fruit,Fruit>", "iVariance<Animal,Apple,Apple>");
                if (ret)
                {
                    result--;
                }
            }

            dynamic v21 = new Variance<Tiger, Fruit, Fruit>();
            try
            {
                result++;
                dynamic v22 = (iVariance<Animal, Apple, Apple>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference001.typeinference001
{
    public // <Area>variance</Area>
           // <Title> </Title>
           // <Description> variance on generics </Description>
           // <RelatedBugs></RelatedBugs>
           // <Expects status=success></Expects>
           // <Code>
class Animal
    {
    }

    public class Mammal : Animal
    {
    }

    public interface R<out T>
    {
    }

    public interface W<in T>
    {
    }

    public interface X<T>
    {
    }

    public class C<T> : R<T>, W<T>, X<T>
    {
    }

    public class Program
    {
        public static int w<T>(T a, W<T> b, string actualType)
        {
            if (typeof(T).ToString() == actualType)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int ret = 0;
            Mammal m = new Mammal();
            dynamic ca = new C<Animal>();
            ret += w(m, ca, "ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference001.typeinference001.Animal"); // This now infers mammal. It previously inferred Animal
            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference002.typeinference002
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    using System;

    public class C
    {
        public int Status = 1;
        public void Foo<T>(T x, Action<T> y) //where T : new()
        {
            this.Status = 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var c = new C();
            var action = (Action<object>)Console.WriteLine;
            int rez = 0;
            c.Status = 1;
            c.Foo("", action); // Prints System.Object
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo("", action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference003.typeinference003
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public class C
    {
        public int Status = 1;
        public void Foo<T>(T x, MyDel<T> y) where T : new()
        {
            this.Status = 0;
        }

        public delegate void MyDel<in T>(T t);
        private static void Foo(object o)
        {
        }

        private static void Foo(string o)
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var c = new C();
            var action = new MyDel<dynamic>(Foo);
            c.Status = 1;
            c.Foo("", action);
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo("", action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference004.typeinference004
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public interface IFoo<in T>
    {
        void Foo(T t);
    }

    public class D<T> : IFoo<T>
    {
        public void Foo(T t)
        {
            System.Console.WriteLine(1);
        }
    }

    public class C
    {
        public int Status = 0;
        public void Foo<T>(T x, IFoo<T> y) where T : new()
        {
            this.Status = 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var c = new C();
            var action = new D<object>();
            c.Status = 1;
            c.Foo("", action);
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo("", action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference005.typeinference005
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public interface IFoo<in T>
    {
        void Foo(T t);
    }

    public class D<T> : IFoo<T>
    {
        public void Foo(T t)
        {
            System.Console.WriteLine(1);
        }
    }

    public class C
    {
        public int Status = 0;
        public void Foo<T>(T x, IFoo<T> y) where T : class
        {
            this.Status = 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var c = new C();
            var action = new D<object>();
            c.Status = 1;
            c.Foo("", action);
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo("", action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference006.typeinference006
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public interface IFoo<in T>
    {
        void Foo(T t);
    }

    public class D<T> : IFoo<T>
    {
        public void Foo(T t)
        {
            System.Console.WriteLine(1);
        }
    }

    public interface IFoo
    {
    }

    public class F : IFoo
    {
    }

    public class C
    {
        public int Status = 0;
        public void Foo<T>(T x, IFoo<T> y) where T : IFoo
        {
            this.Status = 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var c = new C();
            var action = new D<IFoo>();
            c.Status = 1;
            c.Foo(new F(), action);
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo(new F(), action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference007.typeinference007
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    using System;

    public class C
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            Action<Func<object>, Action<string>> x = (s, o) =>
            {
            }

            ;
            C.Status = 1;
            Foo(x); // System.Object
            rez += C.Status;
            C.Status = 1;
            Foo((dynamic)x); // System.String
            rez += C.Status;
            return rez;
        }

        public static void Foo<T>(Action<Func<T>, Action<T>> x)
        {
            C.Status = 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference008.typeinference008
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        public static int Status = 0;
        public static implicit operator string (A a)
        {
            return "";
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            A.Status = 1;
            Foo(new A(), (Action<string>)Console.WriteLine);
            rez += A.Status;
            A.Status = 1;
            Foo(new A(), (dynamic)(Action<string>)Console.WriteLine); // RuntimeBinderException
            rez += A.Status;
            return rez;
        }

        public static void Foo<T>(T x, Action<T> y)
        {
            A.Status = 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.complex.typeinference009.typeinference009
{
    // <Area>variance</Area>
    // <Title> </Title>
    // <Description> variance on generics </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    using System;

    public class C
    {
        public int Status = 0;
        public void Foo<T>(T x, Action<T> y) where T : new()
        {
            this.Status = 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            var c = new C();
            var action = (Action<object>)Console.WriteLine;
            c.Status = 1;
            c.Foo("", action); // Prints System.Object
            rez += c.Status;
            c.Status = 1;
            ((dynamic)c).Foo("", action); // RuntimeBinderException
            rez += c.Status;
            return rez;
        }
    }
    // </Code>
}
