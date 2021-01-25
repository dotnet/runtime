// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.gettype01.gettype01
{
    // <Area>variance</Area>
    // <Title> Expression based tests</Title>
    // <Description> getType tests</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            iVariance<Animal> v2 = v1;
            dynamic v3 = (iVariance<Animal>)v1;
            if (typeof(Variance<Tiger>).ToString() != v2.GetType().ToString())
                return 1;
            if (typeof(Variance<Tiger>).ToString() != v3.GetType().ToString())
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.gettype02.gettype02
{
    // <Area>variance</Area>
    // <Title> Expression based tests</Title>
    // <Description> getType tests</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<in T>
    {
        void Boo(T t);
    }

    public class Variance<T> : iVariance<T>
    {
        public void Boo(T t)
        {
        }
    }

    public class Animal
    {
    }

    public class Tiger : Animal
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
            dynamic v1 = new Variance<Animal>();
            iVariance<Tiger> v2 = v1;
            dynamic v3 = (iVariance<Tiger>)v1;
            if (typeof(Variance<Animal>).ToString() != v2.GetType().ToString())
                return 1;
            if (typeof(Variance<Animal>).ToString() != v3.GetType().ToString())
                return 1;
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.gettype03.gettype03
{
    // <Area>variance</Area>
    // <Title> Expression based tests</Title>
    // <Description> getType tests</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate void Foo<in T>(T t);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            Foo<Tiger> f2 = f1;
            dynamic f3 = (Foo<Tiger>)f1;
            if (typeof(Foo<Animal>).ToString() != f2.GetType().ToString())
                return 1;
            if (typeof(Foo<Animal>).ToString() != f3.GetType().ToString())
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.gettype04.gettype04
{
    // <Area>variance</Area>
    // <Title> Expression based tests</Title>
    // <Description> getType tests</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate T Foo<out T>();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            Foo<Animal> f2 = f1;
            dynamic f3 = (Foo<Animal>)f1;
            if (typeof(Foo<Tiger>).ToString() != f2.GetType().ToString())
                return 1;
            if (typeof(Foo<Tiger>).ToString() != f3.GetType().ToString())
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is01.is01
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a covariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            iVariance<Animal> v2 = v1;
            dynamic v3 = (iVariance<Animal>)v1;
            if (!(v2 is iVariance<Animal>))
                return 1;
            if (!(v3 is iVariance<Animal>))
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is02.is02
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a covariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Tiger> v1 = new Variance<Tiger>();
            iVariance<Animal> v2 = v1;
            dynamic v3 = (iVariance<Animal>)v1;
            if (!(v2 is iVariance<Animal>))
                return 1;
            if (!(v3 is iVariance<Animal>))
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is03.is03
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with contravariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate void Foo<in T>(T t);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            Foo<Tiger> f2 = f1;
            dynamic f3 = (Foo<Tiger>)f1;
            if (!(f2 is Foo<Tiger>))
                return 1;
            if (!(f3 is Foo<Tiger>))
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is04.is04
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with contravariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        private delegate void Foo<in T>(T t);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo<Animal> f1 = (Animal a) =>
            {
            }

            ;
            Foo<Tiger> f2 = f1;
            dynamic f3 = (Foo<Tiger>)f1;
            if (!(f2 is Foo<Animal>))
                return 1;
            if (!(f3 is Foo<Animal>))
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is05.is05
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a covariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            iVariance<Animal> v2 = v1;
            if (v1 is iVariance<Animal>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is06.is06
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a covariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            iVariance<Animal> v2 = v1;
            if (v1 is iVariance<Tiger>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is07.is07
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with contravariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate void Foo<in T>(T t);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            Foo<Tiger> f2 = f1;
            if (f1 is Foo<Tiger>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is08.is08
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with contravariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate void Foo<in T>(T t);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            Foo<Tiger> f2 = f1;
            if (f1 is Foo<Animal>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is09.is09
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a invariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            if (v1 is iVariance<Animal>)
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is10.is10
{
    // <Area>variance</Area>
    // <Title> expressions - Is Keyword</Title>
    // <Description> using the is keyword on a invariant public interface </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : new()
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Tiger>();
            if (v1 is iVariance<Tiger>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is11.is11
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with contravariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        private delegate void Foo<T>(T t);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            if (f1 is Foo<Tiger>)
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.expr.is12.is12
{
    // <Area>variance</Area>
    // <Title> expressions - is keyword</Title>
    // <Description> is keyword with invariant delegates</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        private delegate void Foo<T>(T t);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f1 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            if (f1 is Foo<Animal>)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}
