// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.order1.order1
{
    // <Area>variance</Area>
    // <Title> Order of Basic covariance on interfaces</Title>
    // <Description> basic coavariance on interfaces with many parameters. Order does not matter</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<S, out T, U, V>
    {
        T Boo();
    }

    public class Variance<S, T, U, V> : iVariance<S, T, U, V> where T : new()
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
            dynamic v11 = new Variance<int, Tiger, string, long>();
            iVariance<int, Animal, string, long> v12 = v11;
            var x1 = v12.Boo();
            Variance<int, Tiger, string, long> v21 = new Variance<int, Tiger, string, long>();
            dynamic v22 = (iVariance<int, Animal, string, long>)v21;
            var x2 = v22.Boo();
            dynamic v31 = new Variance<int, Tiger, string, long>();
            dynamic v32 = (iVariance<int, Animal, string, long>)v31;
            var x3 = v32.Boo();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.order2.order2
{
    // <Area>variance</Area>
    // <Title> Order of Basic covariance on interfaces</Title>
    // <Description> basic coavariance on interfaces with many parameters.
    // A second parameter is also used in a covariant way but is not covariant itself. this should fail
    //</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<S, out T, U, V>
    {
        T Boo();
    }

    public class Variance<S, T, U, V> : iVariance<S, T, U, V> where T : new()
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
            int result = 0, count = 0;
            dynamic v11 = new Variance<int, Tiger, string, Tiger>();
            try
            {
                result++;
                iVariance<int, Animal, string, Animal> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<int,Tiger,string,Tiger>", "iVariance<int,Animal,string,Animal>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<int, Tiger, string, Tiger> v21 = new Variance<int, Tiger, string, Tiger>();
            try
            {
                result++;
                dynamic v22 = (iVariance<int, Animal, string, Animal>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic v31 = new Variance<int, Tiger, string, Tiger>();
            try
            {
                result++;
                dynamic v32 = (iVariance<int, Animal, string, Animal>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.order3.order3
{
    // <Area>variance</Area>
    // <Title> Order of Basic covariance on delegates</Title>
    // <Description> basic coavariance on delegates with many parameters. Order does not matter</Description>
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
        public delegate T Foo<S, out T, U, V>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f11 = (Foo<int, Tiger, string, long>)(() =>
            {
                return new Tiger();
            }

            );
            Foo<int, Animal, string, long> f12 = f11;
            Tiger t1 = f11();
            Foo<int, Tiger, string, long> f21 = () =>
            {
                return new Tiger();
            }

            ;
            dynamic f22 = (Foo<int, Animal, string, long>)f21;
            Tiger t2 = f22();
            dynamic f31 = (Foo<int, Tiger, string, long>)(() =>
            {
                return new Tiger();
            }

            );
            dynamic f32 = (Foo<int, Animal, string, long>)f31;
            Tiger t3 = f31();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.order4.order4
{
    // <Area>variance</Area>
    // <Title> Order of Basic covariance on delegates</Title>
    // <Description> basic coavariance on delegates with many parameters. only one type param is variant</Description>
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
        public delegate T Foo<S, out T, U, V>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            bool ret = true;
            dynamic f11 = (Foo<int, Tiger, string, Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            try
            {
                result++;
                Foo<int, Animal, string, Animal> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                result--;
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "C.Foo<int,Tiger,string,Tiger>", "C.Foo<int,Animal,string,Animal>");
                if (!ret)
                    result++;
            }

            Foo<int, Tiger, string, Tiger> f21 = () =>
            {
                return new Tiger();
            }

            ;
            try
            {
                result++;
                dynamic f22 = (Foo<int, Animal, string, Animal>)f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                result--;
                ret = ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "C.Foo<int,Tiger,string,Tiger>", "C.Foo<int,Animal,string,Animal>");
                if (!ret)
                    result++;
            }

            dynamic f31 = (Foo<int, Tiger, string, Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            try
            {
                result++;
                dynamic f32 = (Foo<int, Animal, string, Animal>)f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                result--;
                ret = ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "C.Foo<int,Tiger,string,Tiger>", "C.Foo<int,Animal,string,Animal>");
                if (!ret)
                    result++;
            }

            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.partial01.partial01
{
    // <Area>variance</Area>
    // <Title> Basic covariance on delegate types </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type in a partial public class </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public partial class C
    {
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
            Foo<Animal> f2 = f1;
            Animal t = f2();
            return 0;
        }
    }

    public partial class C
    {
        private delegate T Foo<out T>();
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.valtype01.valtype01
{
    // <Area>variance</Area>
    // <Title> Value types</Title>
    // <Description> basic coavariance on interfaces with value types </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int count = 0, result = 0;
            dynamic v11 = new Variance<uint>();
            try
            {
                result++;
                iVariance<int> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<uint>", "iVariance<int>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<uint> v21 = new Variance<uint>();
            try
            {
                result++;
                dynamic v22 = (iVariance<int>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic v31 = new Variance<uint>();
            try
            {
                result++;
                dynamic v32 = (iVariance<int>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.valtype02.valtype02
{
    // <Area>variance</Area>
    // <Title> Value types</Title>
    // <Description> basic coavariance on dynamic with value types </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
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
            int count = 0, result = 0;
            bool ret = true;
            dynamic f11 = (Foo<uint>)(() =>
            {
                return 0;
            }

            );
            try
            {
                count++;
                result++;
                Foo<int> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "C.Foo<uint>", "C.Foo<int>");
                if (ret)
                {
                    result--;
                }
            }

            //Foo<uint> f21 = () => { return 0; };
            //try
            //{
            //    result++;
            //    dynamic f22 = (Foo<int>)f21;
            //}
            //catch (System.InvalidCastException)
            //{
            //    result--;
            //    System.Console.WriteLine("Scenario {0} passed. ", ++count);
            //}
            dynamic f31 = (Foo<uint>)(() =>
            {
                return 0;
            }

            );
            try
            {
                count++;
                result++;
                dynamic f32 = (Foo<int>)f31;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "C.Foo<uint>", "C.Foo<int>");
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.valtype03.valtype03
{
    // <Area>variance</Area>
    // <Title> Value types</Title>
    // <Description> basic coavariance on interfaces with nullable value types </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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
            dynamic v11 = new Variance<uint>();
            try
            {
                result++;
                iVariance<uint?> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<uint>", "iVariance<uint?>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<uint> v21 = new Variance<uint>();
            try
            {
                result++;
                dynamic v22 = (iVariance<uint?>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic v31 = new Variance<uint>();
            try
            {
                result++;
                dynamic v32 = (iVariance<uint?>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.decl.valtype04.valtype04
{
    // <Area>variance</Area>
    // <Title> Value types</Title>
    // <Description> basic coavariance on delegate with null value types </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
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
            int count = 0, result = 0;
            bool ret = true;
            dynamic f11 = (Foo<uint>)(() =>
            {
                return 0;
            }

            );
            try
            {
                count++;
                result++;
                Foo<uint?> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, ex.Message, "C.Foo<uint>", "C.Foo<uint?>");
                if (ret)
                {
                    result--;
                }
            }

            //Foo<uint> f21 = () => { return 0; };
            //try
            //{
            //    result++;
            //    dynamic f22 = (Foo<uint?>)f21;
            //}
            //catch (System.InvalidCastException)
            //{
            //    result--;
            //    System.Console.WriteLine("Scenario {0} passed. ", ++count);
            //}
            dynamic f31 = (Foo<uint>)(() =>
            {
                return 0;
            }

            );
            try
            {
                count++;
                result++;
                dynamic f32 = (Foo<uint?>)f31;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "C.Foo<uint>", "C.Foo<uint?>");
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
