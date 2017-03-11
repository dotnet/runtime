// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecontravar2.dlgatecontravar2
{
    // <Area>variance</Area>
    // <Title> Basic Error Contravariance on delegates</Title>
    // <Description> basic errorcontravariance on delegates - Incorrect type mismatch</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(42,37\).*CS0168</Expects>
    //<Expects Status=warning>\(53,26\).*CS0168</Expects>
    using System;

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
            int result = 0;
            bool ret = true;
            dynamic f11 = (Foo<Tiger>)((Tiger a) =>
            {
            }

            );
            try
            {
                Foo<Animal> f12 = f11;
                result++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "C.Foo<Tiger>", "C.Foo<Animal>");
                if (ret == false)
                    result++;
            }

            Foo<Tiger> f21 = (Tiger a) =>
            {
            }

            ;
            try
            {
                dynamic f22 = (Foo<Animal>)f21;
                result++;
            }
            catch (InvalidCastException e)
            {
            }

            dynamic f31 = (Foo<Tiger>)((Tiger a) =>
            {
            }

            );
            try
            {
                dynamic f32 = (Foo<Animal>)f31;
                result++;
            }
            catch (Exception e)
            {
            }

            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgateconvar.dlgateconvar
{
    // <Area>variance</Area>
    // <Title> Basic Contravariance on delegates</Title>
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
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic f11 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            Foo<Tiger> f12 = (Foo<Tiger>)f11;
            f12(new Tiger());
            Foo<Animal> f21 = (Animal a) =>
            {
            }

            ;
            dynamic f22 = (Foo<Tiger>)f21;
            f22(new Tiger());
            dynamic f31 = (Foo<Animal>)((Animal a) =>
            {
            }

            );
            dynamic f32 = (Foo<Tiger>)f31;
            f32(new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecov.dlgatecov
{
    // <Area>variance</Area>
    // <Title> Basic covariance on delegate types </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type</Description>
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
            dynamic f11 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            Foo<Animal> f12 = f11;
            Animal t1 = f12();
            Foo<Tiger> f21 = () =>
            {
                return new Tiger();
            }

            ;
            dynamic f22 = (Foo<Animal>)f21;
            Animal t2 = f22();
            dynamic f31 = (Foo<Tiger>)(() =>
            {
                return new Tiger();
            }

            );
            dynamic f32 = (Foo<Animal>)f31;
            Animal t3 = f32();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecovar2.dlgatecovar2
{
    // <Area>variance</Area>
    // <Title> Error- Basic covariance on delegate types </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type, incorrect types</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
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
            int result = 0;
            bool ret = true;
            dynamic f11 = (Foo<Animal>)(() =>
            {
                return new Tiger();
            }

            );
            try
            {
                result++;
                Foo<Tiger> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                result--;
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "C.Foo<Animal>", "C.Foo<Tiger>");
                if (!ret)
                    result++;
            }

            Foo<Animal> f21 = () =>
            {
                return new Tiger();
            }

            ;
            try
            {
                result++;
                dynamic f22 = (Foo<Tiger>)f21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic f31 = (Foo<Animal>)(() =>
            {
                return new Tiger();
            }

            );
            try
            {
                result++;
                dynamic f32 = (Foo<Tiger>)f31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecontravar2.integeregererfacecontravar2
{
    // <Area>variance</Area>
    // <Title> Basic error contravariance on interfaces</Title>
    // <Description> basic error contravariance on interfaces - wrong type assignments </Description>
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
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int count = 0;
            int result = 0;
            dynamic v11 = new Variance<Tiger>();
            try
            {
                result++;
                iVariance<Animal> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<Tiger>", "iVariance<Animal>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<Tiger> v21 = new Variance<Tiger>();
            try
            {
                result++;
                dynamic v22 = (iVariance<Animal>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            Variance<Tiger> v31 = new Variance<Tiger>();
            try
            {
                result++;
                dynamic v32 = (iVariance<Animal>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfaceconvar.integeregererfaceconvar
{
    // <Area>variance</Area>
    // <Title> Basic contravariance on interfaces</Title>
    // <Description> basic contravariance on interfaces </Description>
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
            dynamic v11 = new Variance<Animal>();
            iVariance<Tiger> v12 = v11;
            v12.Boo(new Tiger());
            Variance<Animal> v21 = new Variance<Animal>();
            dynamic v22 = (iVariance<Tiger>)v21;
            v22.Boo(new Tiger());
            Variance<Animal> v31 = new Variance<Animal>();
            dynamic v32 = (iVariance<Tiger>)v31;
            v32.Boo(new Tiger());
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecovar2.integeregererfacecovar2
{
    // <Area>variance</Area>
    // <Title> Basic error covariance on interfaces</Title>
    // <Description> basic error coavariance on interfaces - type mismatch</Description>
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
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            dynamic v11 = new Variance<Animal>();
            try
            {
                result++;
                iVariance<Tiger> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<Animal>", "iVariance<Tiger>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<Animal> v21 = new Variance<Animal>();
            try
            {
                result++;
                dynamic v22 = (iVariance<Tiger>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic v31 = new Variance<Animal>();
            try
            {
                result++;
                dynamic v32 = (iVariance<Tiger>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecovar.integeregererfacecovar
{
    // <Area>variance</Area>
    // <Title> Basic covariance on interfaces</Title>
    // <Description> basic coavariance on interfaces </Description>
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
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Tiger> v11 = new Variance<Tiger>();
            iVariance<Animal> v12 = v11;
            var x1 = v12.Boo();
            dynamic v21 = new Variance<Tiger>();
            iVariance<Animal> v22 = v21;
            var x2 = v22.Boo();
            dynamic v31 = new Variance<Tiger>();
            dynamic v32 = (iVariance<Animal>)v31;
            var x3 = v32.Boo();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecontravar4.dlgatecontravar4
{
    // <Area>variance</Area>
    // <Title> Basic Error Contravariance on delegates</Title>
    // <Description> basic errorcontravariance on delegates - Incorrect type mismatch</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(40,37\).*CS0168</Expects>
    //<Expects Status=warning>\(51,37\).*CS0168</Expects>
    using System;

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
            int result = 0;
            bool ret = true;
            dynamic f11 = (Foo<string>)((string a) =>
            {
            }

            );
            try
            {
                Foo<dynamic> f12 = f11;
                result++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "C.Foo<string>", "C.Foo<object>");
                if (!ret)
                    result++;
            }

            Foo<string> f21 = (string a) =>
            {
            }

            ;
            try
            {
                dynamic f22 = (Foo<dynamic>)f21;
                result++;
            }
            catch (InvalidCastException e)
            {
            }

            dynamic f31 = (Foo<string>)((string a) =>
            {
            }

            );
            try
            {
                dynamic f32 = (Foo<dynamic>)f31;
                result++;
            }
            catch (InvalidCastException e)
            {
            }

            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgateconvar3.dlgateconvar3
{
    // <Area>variance</Area>
    // <Title> Basic Contravariance on delegates</Title>
    // <Description> basic contravariance on delegates </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
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
            dynamic f11 = (Foo<dynamic>)((dynamic a) =>
            {
            }

            );
            Foo<string> f12 = (Foo<string>)f11;
            f12(string.Empty);
            Foo<dynamic> f21 = (dynamic a) =>
            {
            }

            ;
            dynamic f22 = (Foo<string>)f21;
            f22(null);
            dynamic f31 = (Foo<dynamic>)((dynamic a) =>
            {
            }

            );
            dynamic f32 = (Foo<string>)f31;
            f32("ABC");
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecov3.dlgatecov3
{
    // <Area>variance</Area>
    // <Title> Basic covariance on delegate types </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
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
            dynamic f11 = (Foo<string>)(() =>
            {
                return null;
            }

            );
            Foo<dynamic> f12 = f11;
            dynamic t1 = f12();
            Foo<string> f21 = () =>
            {
                return string.Empty;
            }

            ;
            dynamic f22 = (Foo<dynamic>)f21;
            dynamic t2 = f22();
            dynamic f31 = (Foo<string>)(() =>
            {
                return "ABC";
            }

            );
            dynamic f32 = (Foo<dynamic>)f31;
            dynamic t3 = f32();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.dlgatecovar4.dlgatecovar4
{
    // <Area>variance</Area>
    // <Title> Error- Basic covariance on delegate types </Title>
    // <Description> Having a covariant delegate and assigning it to a bigger type, incorrect types</Description>
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
            int result = 0;
            bool ret = true;
            dynamic f11 = (Foo<dynamic>)(() =>
            {
                return 10;
            }

            );
            try
            {
                result++;
                Foo<C> f12 = f11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                result--;
                ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "C.Foo<object>", "C.Foo<C>");
                if (!ret)
                    result++;
            }

            Foo<dynamic> f21 = () =>
            {
                return 10;
            }

            ;
            try
            {
                result++;
                dynamic f22 = (Foo<C>)f21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic f31 = (Foo<dynamic>)(() =>
            {
                return 10;
            }

            );
            try
            {
                result++;
                dynamic f32 = (Foo<C>)f31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecontravar4.integeregererfacecontravar4
{
    // <Area>variance</Area>
    // <Title> Basic error contravariance on interfaces</Title>
    // <Description> basic error contravariance on interfaces - wrong type assignments </Description>
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
            dynamic v11 = new Variance<string>();
            try
            {
                result++;
                iVariance<dynamic> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<string>", "iVariance<object>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<string> v21 = new Variance<string>();
            try
            {
                result++;
                dynamic v22 = (iVariance<dynamic>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            Variance<string> v31 = new Variance<string>();
            try
            {
                result++;
                dynamic v32 = (iVariance<dynamic>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfaceconvar3.integeregererfaceconvar3
{
    // <Area>variance</Area>
    // <Title> Basic contravariance on interfaces</Title>
    // <Description> basic contravariance on interfaces </Description>
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic v11 = new Variance<dynamic>();
            iVariance<string> v12 = v11;
            v12.Boo(string.Empty);
            Variance<dynamic> v21 = new Variance<dynamic>();
            dynamic v22 = (iVariance<string>)v21;
            v22.Boo(null);
            Variance<dynamic> v31 = new Variance<dynamic>();
            dynamic v32 = (iVariance<string>)v31;
            v32.Boo("ABC");
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecovar4.integeregererfacecovar4
{
    // <Area>variance</Area>
    // <Title> Basic error covariance on interfaces</Title>
    // <Description> basic error coavariance on interfaces - type mismatch</Description>
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
            dynamic v11 = new Variance<object>();
            try
            {
                result++;
                iVariance<C> v12 = v11;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, ex.Message, "Variance<object>", "iVariance<C>");
                if (ret)
                {
                    result--;
                }
            }

            Variance<object> v21 = new Variance<object>();
            try
            {
                result++;
                dynamic v22 = (iVariance<C>)v21;
            }
            catch (System.InvalidCastException)
            {
                result--;
            }

            dynamic v31 = new Variance<object>();
            try
            {
                result++;
                dynamic v32 = (iVariance<C>)v31;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.basic.integeregererfacecovar3.integeregererfacecovar3
{
    // <Area>variance</Area>
    // <Title> Basic covariance on interfaces</Title>
    // <Description> basic coavariance on interfaces </Description>
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

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<C> v11 = new Variance<C>();
            iVariance<dynamic> v12 = v11;
            var x1 = v12.Boo();
            dynamic v21 = new Variance<C>();
            iVariance<dynamic> v22 = v21;
            var x2 = v22.Boo();
            dynamic v31 = new Variance<C>();
            dynamic v32 = (iVariance<dynamic>)v31;
            var x3 = v32.Boo();
            return 0;
        }
    }
    //</Code>
}
