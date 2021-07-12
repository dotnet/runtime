// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.as01.as01
{
    // <Area>variance</Area>
    // <Title> As keyword in variance </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type through the as keyword</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
        public virtual string speakName()
        {
            return "Animal";
        }
    }

    public class Mammal : Animal
    {
        public override string speakName()
        {
            return "Mammal";
        }
    }

    public class Tiger : Mammal
    {
        public override string speakName()
        {
            return "Tiger";
        }
    }

    public class Giraffe : Mammal
    {
        public static explicit operator Tiger(Giraffe g)
        {
            return new Tiger();
        }

        public override string speakName()
        {
            return "Giraffe";
        }
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
            int result = 0;
            // scenario 1
            dynamic f11 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            Foo<Animal> f12 = f11 as Foo<Tiger>;
            Animal t1 = f12();
            if (t1.speakName() != "Tiger")
                result++;
            // scenario 2
            Foo<Tiger> f21 = () =>
            {
                return new Tiger();
            }

            ;
            dynamic f22 = (Foo<Animal>)(f21 as Foo<Tiger>);
            Animal t2 = f22();
            if (t2.speakName() != "Tiger")
                result++;
            // scenario 3
            dynamic f31 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            dynamic f32 = (Foo<Animal>)(f31 as Foo<Tiger>);
            Animal t3 = f32();
            if (t3.speakName() != "Tiger")
                result++;
            return result;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment01.assignment01
{
    // <Area>variance</Area>
    // <Title> assignment with variance </Title>
    // <Description> assigning to a property type with an interface</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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
        public static iVariance<Animal> p1
        {
            get;
            set;
        }

        public static dynamic p2
        {
            get;
            set;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Tiger> v1 = new Variance<Tiger>();
            dynamic v2 = new Variance<Tiger>();
            try
            {
                p1 = v2;
                p1.Boo();
                p2 = v1;
                p2.Boo();
                p2 = v2;
                p2.Boo();
            }
            catch (System.Exception) // should NOT throw out runtime exception so we needn't check the error message
            {
                return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment02.assignment02
{
    // <Area>variance</Area>
    // <Title> assignment with variance </Title>
    // <Description> assigning to a property type with an interface</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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
        public static iVariance<Tiger> p1
        {
            get;
            set;
        }

        public static iVariance<Tiger> p2
        {
            get;
            set;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Animal> v1 = new Variance<Animal>();
            dynamic v2 = new Variance<Animal>();
            try
            {
                p1 = v2;
                p1.Boo(new Tiger());
                p2 = v1;
                p2.Boo(new Tiger());
                p2 = v2;
                p2.Boo(new Tiger());
            }
            catch (System.Exception) // should NOT throw out runtime exception so we needn't check the error message
            {
                return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment03.assignment03
{
    // <Area>variance</Area>
    // <Title> Assignment to a property</Title>
    // <Description> basic contravariance on delegates assigning to a property </Description>
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
        public static Foo<Tiger> p1
        {
            get;
            set;
        }

        public static dynamic p2
        {
            get;
            set;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Foo<Animal> v1 = (Animal a) =>
            {
            }

            ;
            dynamic v2 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            try
            {
                p1 = v2;
                p1(new Tiger());
                p2 = v1;
                p2(new Tiger());
                p2 = v2;
                p2(new Tiger());
            }
            catch (System.Exception) // should NOT throw out runtime exception so we needn't check the error message
            {
                return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment04.assignment04
{
    // <Area>variance</Area>
    // <Title> Assignment to an event </Title>
    // <Description> Having a covariant delegate and assigning it to an event</Description>
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
        public static event Foo<Animal> e;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            e += f;
            Animal t = e();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment05.assignment05
{
    // <Area>variance</Area>
    // <Title> assignment of Contravariance to events</Title>
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

    public class C
    {
        public delegate void Foo<in T>(T t);
        public static event Foo<Tiger> f2;
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
            f2 += f1;
            f2(new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment06.assignment06
{
    // <Area>variance</Area>
    // <Title> assignment of covariant types to an array</Title>
    // <Description> assignment of covariant types to an array </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    //<Expects Status=warning>\(22,29\).*CS0649</Expects>
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
        public static iVariance<Animal>[] field1;
        public static dynamic[] field2;
        public static dynamic field3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Tiger> v1 = new Variance<Tiger>();
            dynamic v2 = new Variance<Tiger>();
            field1 = new iVariance<Animal>[1];
            field1[0] = v2;
            var x = field1[0].Boo();
            //field2 = new iVariance<Animal>[1];
            //field2[0] = v1;
            //field2[0].Boo();
            //field2[0] = v2;
            //field2[0].Boo();
            field3 = new iVariance<Animal>[1];
            field3[0] = v1;
            field3[0].Boo();
            field3[0] = v2;
            field3[0].Boo();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment07.assignment07
{
    using System;
    
    // <Area>variance</Area>
    // <Title> assignment Contravariant delegates</Title>
    // <Description> contravariance on delegates assigned to arrays</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    //<Expects Status=warning>\(16,22\).*CS0169</Expects>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class C
    {
        public delegate void Foo<in T>(T t);
        private static Foo<Tiger>[] s_array1;
        private static dynamic[] s_array2;
        private static dynamic s_array3;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55119", typeof(PlatformDetection), nameof(PlatformDetection.IsLinqExpressionsBuiltWithIsInterpretingOnly))]
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
            dynamic f2 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            s_array1 = new Foo<Tiger>[1];
            s_array1[0] = f2;
            s_array1[0](new Tiger());
            //array2 = new Foo<Tiger>[1];
            //array2[0] = f1;
            //array2[0](new Tiger());
            //array2[0] = f2;
            //array2[0](new Tiger());
            s_array3 = new Foo<Tiger>[1];
            s_array3[0] = f1;
            s_array3[0](new Tiger());
            s_array3[0] = f2;
            s_array3[0](new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment08.assignment08
{
    // <Area>variance</Area>
    // <Title> Basic covariance on delegate types passed to a function </Title>
    // <Description> Having a covariant delegate and passing it to a function</Description>
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
            Foo<Tiger> f1 = () =>
            {
                return new Tiger();
            }

            ;
            dynamic f2 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            try
            {
                Bar1(f2);
                Bar2(f1);
                Bar2(f2);
            }
            catch (System.Exception) // should NOT throw out runtime exception so we needn't check the error message
            {
                return 1;
            }

            return 0;
        }

        public static void Bar1(Foo<Animal> f)
        {
        }

        public static void Bar2(dynamic f)
        {
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment09.assignment09
{
    // <Area>variance</Area>
    // <Title> contravariance on interfaces and passing to a method</Title>
    // <Description> calling methods with public interface contravariance </Description>
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
            Variance<Animal> v1 = new Variance<Animal>();
            dynamic v2 = new Variance<Animal>();
            try
            {
                Bar1(v2);
                Bar2(v1);
                Bar2(v2);
            }
            catch (System.Exception) // should NOT throw out runtime exception so we needn't check the error message
            {
                return 1;
            }

            return 0;
        }

        public static void Bar1(iVariance<Tiger> f)
        {
        }

        public static void Bar2(dynamic f)
        {
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment10.assignment10
{
    // <Area>variance</Area>
    // <Title> contravariance on interfaces and passing to a method</Title>
    // <Description> calling methods with public interface contravariance </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
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
            IA<object> x = new A();
            var y = new B();
            y.Foo(x);
            dynamic z = y;
            z.Foo(x);
            return 0;
        }

        public void Foo(IA<string> x)
        {
        }
    }

    public interface IA<in T>
    {
    }

    public class A : IA<object>
    {
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.assignment11.assignment11
{
    // <Area>variance</Area>
    // <Title> contravariance on interfaces and passing to a method</Title>
    // <Description> calling methods with public interface contravariance </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
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
            IA<dynamic> x = new A();
            var y = new B();
            y.Foo(x);
            dynamic z = y;
            z.Foo(x);
            return 0;
        }

        public void Foo(IA<string> x)
        {
        }
    }

    public interface IA<in T>
    {
    }

    public class A : IA<object>
    {
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.overload01.overload01
{
    // <Area>variance</Area>
    // <Title> Operator overloading in variance </Title>
    // <Description> Having a covariant delegate and assigning it to a same level type with variance and implicit operator overloading</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
        public virtual string speakName()
        {
            return "Animal";
        }
    }

    public class Mammal : Animal
    {
        public override string speakName()
        {
            return "Mammal";
        }
    }

    public class Tiger : Mammal
    {
        public override string speakName()
        {
            return "Tiger";
        }
    }

    public class Giraffe : Mammal
    {
        public static implicit operator Tiger(Giraffe g)
        {
            return new Tiger();
        }

        public override string speakName()
        {
            return "Giraffe";
        }
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
            int result = 0, count = 0;
            dynamic f11 = (Foo<Giraffe>)(() =>
            {
                return new Giraffe();
            }

            );
            try
            {
                result++;
                Foo<Tiger> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "C.Foo<Giraffe>", "C.Foo<Tiger>");
                if (ret)
                {
                    result--;
                }
            }

            //Foo<Giraffe> f21 = () => { return new Giraffe(); };
            //try
            //{
            //    result++;
            //    dynamic f22 = (Foo<Tiger>)f21;
            //}
            //catch (System.InvalidCastException)
            //{
            //    result--;
            //    System.Console.WriteLine("Scenario {0} passed.", ++count);
            //}
            dynamic f31 = (Foo<Giraffe>)(() =>
            {
                return new Giraffe();
            }

            );
            try
            {
                result++;
                dynamic f32 = (Foo<Tiger>)f31;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "C.Foo<Giraffe>", "C.Foo<Tiger>");
                if (ret)
                {
                    result--;
                }
            }

            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.overload02.overload02
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution in contravariant interfaces</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class Bear : Mammal
    {
    }

    public class C
    {
        public static int Bar(iVariance<Tiger> t)
        {
            return 2;
        }

        public static int Bar(iVariance<Bear> t)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Animal>();
            int result = 0;
            try
            {
                result = C.Bar(v1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "C.Bar(iVariance<Tiger>)", "C.Bar(iVariance<Bear>)");
                if (ret)
                {
                    return 0;
                }
            }

            //System.Console.WriteLine(result);
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.overload03.overload03
{
    // <Area>variance</Area>
    // <Title> Operator overloading in variance </Title>
    // <Description> Having a contravariant delegate and assigning it to a bigger type with variance and explicit operator overloading</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
        public virtual string speakName()
        {
            return "Animal";
        }
    }

    public class Mammal : Animal
    {
        public override string speakName()
        {
            return "Mammal";
        }
    }

    public class Tiger : Mammal
    {
        public override string speakName()
        {
            return "Tiger";
        }
    }

    public class Giraffe : Mammal
    {
        public static explicit operator Tiger(Giraffe g)
        {
            return new Tiger();
        }

        public override string speakName()
        {
            return "Giraffe";
        }
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
            Foo<Animal> f10 = (Animal a) =>
            {
            }

            ;
            dynamic f11 = (Foo<Tiger>)f10;
            Foo<Giraffe> f12 = (Foo<Giraffe>)f11;
            f11(new Tiger());
            Foo<Animal> f20 = (Animal a) =>
            {
            }

            ;
            dynamic f21 = (Foo<Tiger>)f20;
            dynamic f22 = (Foo<Giraffe>)f21;
            f22(new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.overload04.overload04
{
    // <Area>variance</Area>
    // <Title> Operator overloading in variance </Title>
    // <Description> Having a contravariant delegate and assigning it to a similar type with variance and implicit operator overloading</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
        public virtual string speakName()
        {
            return "Animal";
        }
    }

    public class Mammal : Animal
    {
        public override string speakName()
        {
            return "Mammal";
        }
    }

    public class Tiger : Mammal
    {
        public override string speakName()
        {
            return "Tiger";
        }
    }

    public class Giraffe : Mammal
    {
        public static implicit operator Tiger(Giraffe g)
        {
            return new Tiger();
        }

        public override string speakName()
        {
            return "Giraffe";
        }
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
            Foo<Animal> f = (Animal a) =>
            {
            }

            ;
            dynamic f11 = (Foo<Tiger>)f;
            Foo<Giraffe> f12 = (Foo<Giraffe>)f11;
            f11(new Tiger());
            dynamic f21 = (Foo<Tiger>)f;
            dynamic f22 = (Foo<Giraffe>)f21;
            f22(new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.overload05.overload05
{
    // <Area>variance</Area>
    // <Title> Operator overloading in variance </Title>
    // <Description> Having a contravariant public interface and assigning it to a similar type with variance and implicit operator overloading</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public class Animal
    {
        public virtual string speakName()
        {
            return "Animal";
        }
    }

    public class Mammal : Animal
    {
        public override string speakName()
        {
            return "Mammal";
        }
    }

    public class Tiger : Mammal
    {
        public override string speakName()
        {
            return "Tiger";
        }
    }

    public class Giraffe : Mammal
    {
        public static implicit operator Tiger(Giraffe g)
        {
            return new Tiger();
        }

        public override string speakName()
        {
            return "Giraffe";
        }
    }

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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0, count = 0;
            dynamic v11 = new Variance<Tiger>();
            try
            {
                result++;
                iVariance<Giraffe> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<Tiger>", "iVariance<Giraffe>");
                if (ret)
                {
                    result--;
                }
            }

            dynamic v21 = new Variance<Tiger>();
            try
            {
                result++;
                dynamic v22 = (iVariance<Giraffe>)v21;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.resolution01.resolution01
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T>
    {
        public T Boo()
        {
            return default(T);
        }
    }

    public class Animal
    {
    }

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class C
    {
        public static int Bar(iVariance<Animal> t)
        {
            return 2;
        }

        public static int Bar(iVariance<Mammal> t)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = (iVariance<Tiger>)new Variance<Tiger>();
            int result = C.Bar(v1);
            if (result != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.resolution02.resolution02
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution in contravariant interfaces</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class Bear : Mammal
    {
    }

    public class C
    {
        public static int Bar(iVariance<Tiger> t)
        {
            return 2;
        }

        public static int Bar(iVariance<Bear> t)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = new Variance<Animal>();
            int result = 0;
            try
            {
                result = C.Bar(v1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return 0;
            }

            //System.Console.WriteLine(result);
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.resolution03.resolution03
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution in contravariant interfaces</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class Bear : Mammal
    {
    }

    public class C
    {
        public static int Bar(iVariance<Tiger> t)
        {
            return 2;
        }

        public static int Bar(iVariance<Mammal> t)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = (iVariance<Animal>)(new Variance<Animal>());
            int result = C.Bar(v1);
            if (result == 1)
                return 0;
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.resolution04.resolution04
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T>
    {
        public T Boo()
        {
            return default(T);
        }
    }

    public class Animal
    {
    }

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class C
    {
        public int Bar(iVariance<Animal> t)
        {
            return 2;
        }

        public int Bar(iVariance<Mammal> t)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = (iVariance<Tiger>)(new Variance<Tiger>());
            dynamic c = new C();
            var result = c.Bar(v1);
            if (result == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.assign.resolution05.resolution05
{
    // <Area>variance</Area>
    // <Title> overload resolution</Title>
    // <Description> overload resolution </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T>
    {
        public T Boo()
        {
            return default(T);
        }
    }

    public class Animal
    {
    }

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class B
    {
        public static int Bar(iVariance<Mammal> t)
        {
            return 1;
        }
    }

    public class C : B
    {
        public static int Bar(iVariance<Animal> t)
        {
            return 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v1 = (iVariance<Tiger>)(new Variance<Tiger>());
            var result = C.Bar(v1);
            if (result == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}
