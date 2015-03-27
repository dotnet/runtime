// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method001.method001;
    using System;

    public static class Helper
    {
        public static T Cast<T>(dynamic d)
        {
            return (T)d;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method001.method001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method001.method001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic </Title>
    // <Description>
    // non-generic interface with generic member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Foo<T>();
        int Bar();
    }

    public class C : IF1
    {
        int IF1.Foo<T>()
        {
            return 0;
        }

        public int Bar()
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo<int>();
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF1>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method002.method002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method002.method002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic </Title>
    // <Description>
    // non-generic interface with generic member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            C.DynamicCSharpRunTest();
        }
    }

    public interface IF1
    {
        int Foo<T>();
        int Bar();
    }

    public struct C : IF1
    {
        int IF1.Foo<T>()
        {
            return 0;
        }

        public int Foo()
        {
            return 2;
        }

        public int Bar()
        {
            return 1;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo<int>();
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.HasNoTypeVars, ex.Message, "C.Foo()", ErrorVerifier.GetErrorElement(ErrorElementId.SK_METHOD)))
                {
                    error++;
                }
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method003.method003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method003.method003;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic </Title>
    // <Description>
    // generic interface with non-generic member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1<T>
    {
        int Foo();
        int Bar();
    }

    public class C : IF1<int>
    {
        int IF1<int>.Foo()
        {
            return 0;
        }

        public int Foo()
        {
            return 2;
        }

        public int Bar()
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int result = 0;
            int error = 0;
            result = d.Foo();
            if (result != 2)
                error++;
            var x = Helper.Cast<IF1<int>>(d);
            try
            {
                Helper.Cast<IF1<double>>(d);
                error++;
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method004.method004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method004.method004;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic </Title>
    // <Description>
    // generic interface with generic member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1<T>
    {
        int Foo(T t);
        int Bar();
    }

    public class C : IF1<int>
    {
        int IF1<int>.Foo(int i)
        {
            return 0;
        }

        public int Bar()
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo(1);
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF1<int>>(d);
            try
            {
                Helper.Cast<IF1<double>>(d);
                error++;
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method005.method005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.method005.method005;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic </Title>
    // <Description>
    // generic interface with generic member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1<T>
    {
        int Foo<U>();
        int Bar();
    }

    public class C : IF1<int>
    {
        int IF1<int>.Foo<U>()
        {
            return 0;
        }

        public int Bar()
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo<int>();
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF1<int>>(d);
            try
            {
                Helper.Cast<IF1<double>>(d);
                error++;
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.contravariance001.contravariance001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.contravariance001.contravariance001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic : variance</Title>
    // <Description>
    // contra-variance (negative)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public interface IF<in T>
    {
        int Foo(T t);
    }

    public class ContraVar<T> : IF<T>
    {
        int IF<T>.Foo(T t)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new ContraVar<Animal>();
            int error = 0;
            try
            {
                d.Foo();
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "ContraVar<Animal>", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF<Tiger>>(d);
            var y = Helper.Cast<IF<Animal>>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.contravariance002.contravariance002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.contravariance002.contravariance002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic : variance</Title>
    // <Description>
    // contra-variance (negative)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public interface IF<in T>
    {
        int Foo(T t);
    }

    public class ContraVar<T> : IF<T>
    {
        int IF<T>.Foo(T t)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new ContraVar<Tiger>();
            int error = 0;
            try
            {
                d.Foo();
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "ContraVar<Tiger>", "Foo"))
                {
                    error++;
                }
            }

            try
            {
                var x = Helper.Cast<IF<Animal>>(d);
                error++;
            }
            catch (InvalidCastException ex)
            {
            }

            var y = Helper.Cast<IF<Tiger>>(d);
            try
            {
                var z = ((IF<Animal>)d).Foo(new Animal());
                error++;
            }
            catch (InvalidCastException ex)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.covariance001.covariance001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.covariance001.covariance001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic : variance</Title>
    // <Description>
    // co-variance
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public interface IF<out T>
    {
        T Foo();
    }

    public class CoVar<T> : IF<T> where T : new()
    {
        T IF<T>.Foo()
        {
            return new T();
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new CoVar<Tiger>();
            int error = 0;
            try
            {
                d.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "CoVar<Tiger>", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF<Tiger>>(d);
            var y = Helper.Cast<IF<Animal>>(d);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.covariance002.covariance002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Generic.covariance002.covariance002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Generic : variance</Title>
    // <Description>
    // co-variance
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public interface IF<out T>
    {
        T Foo();
    }

    public class CoVar<T> : IF<T> where T : new()
    {
        T IF<T>.Foo()
        {
            return new T();
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new CoVar<Animal>();
            int error = 0;
            try
            {
                d.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
            }

            try
            {
                var x = Helper.Cast<IF<Tiger>>(d);
            }
            catch (InvalidCastException ex)
            {
            }

            var y = Helper.Cast<IF<Animal>>(d);
            try
            {
                var z = ((IF<Tiger>)d).Foo();
            }
            catch (InvalidCastException ex)
            {
            }

            return error;
        }
    }
    // </Code>
}