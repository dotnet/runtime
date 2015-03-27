// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method002.method002;
    using System;

    public static class Helper
    {
        public static T Cast<T>(dynamic d)
        {
            return (T)d;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method002.method002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method002.method002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's base interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF0
    {
        int Foo();
    }

    public interface IF1 : IF0
    {
        int Bar();
    }

    public class C : IF1, IF0
    {
        int IF0.Foo()
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
                result = d.Foo();
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

            var x = Helper.Cast<IF0>(d);
            var y = Helper.Cast<IF1>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method003.method003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method003.method003;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's base interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF0
    {
        int Bar();
    }

    public interface IF1 : IF0
    {
        int Foo();
    }

    public class C : IF1
    {
        int IF1.Foo()
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
                result = d.Foo();
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

            var x = Helper.Cast<IF0>(d);
            var y = Helper.Cast<IF1>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method004.method004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method004.method004;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's derived interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF2 : IF1
    {
    }

    public interface IF1
    {
        int Foo();
        int Bar();
    }

    public class C : IF1
    {
        int IF1.Foo()
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
                result = d.Foo();
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

            try
            {
                var x = Helper.Cast<IF2>(d);
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method005.method005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method005.method005;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's derived interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF2 : IF0
    {
    }

    public interface IF1 : IF0
    {
        new int Foo();
    }

    public interface IF0
    {
        int Foo();
        int Bar();
    }

    public class C : IF1
    {
        int IF1.Foo()
        {
            return 0;
        }

        int IF0.Foo()
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
            try
            {
                result = d.Foo();
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

            try
            {
                var x = Helper.Cast<IF2>(d);
            }
            catch (InvalidCastException)
            {
            }

            try
            {
                result = ((IF2)d).Foo();
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method006.method006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method006.method006;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's derived interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF2 : IF0
    {
    }

    public interface IF1 : IF0
    {
        new int Foo();
    }

    public interface IF0
    {
        int Foo();
        int Bar();
    }

    public class C : IF1, IF2
    {
        int IF1.Foo()
        {
            return 0;
        }

        int IF0.Foo()
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
            try
            {
                result = d.Foo();
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

            var x = Helper.Cast<IF2>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method007.method007
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method007.method007;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's derived interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Foo();
        int Bar();
    }

    public class C : D, IF1
    {
        int IF1.Foo()
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
            dynamic d = new D();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo();
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "D", "Foo"))
                {
                    error++;
                }
            }

            try
            {
                var x = Helper.Cast<IF1>(d);
            }
            catch (InvalidCastException)
            {
            }

            try
            {
                result = ((IF1)d).Foo();
            }
            catch (InvalidCastException)
            {
            }

            return error;
        }
    }

    public class D
    {
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method008.method008
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method008.method008;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // d is a instance of C's derived class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Foo();
        int Bar();
    }

    public class C : IF1
    {
        int IF1.Foo()
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
            dynamic d = new D();
            int result = 0;
            int error = 0;
            try
            {
                result = d.Foo();
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "D", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF1>(d);
            return error;
        }
    }

    public class D : C
    {
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.partialclass003.partialclass003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.partialclass003.partialclass003;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> partial class </Title>
    // <Description>
    // absolutely no implemented member
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(37,13\).*CS0219</Expects>
    using System;

    public interface IF1
    {
        void Foo();
        int Bar();
    }

    public partial class C
    {
    }

    public partial class C : IF1
    {
        void IF1.Foo()
        {
            System.Console.WriteLine("IF1.Foo");
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
                d.Foo();
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.partialintegeregererface001.partialintegeregererface001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.partialintegeregererface001.partialintegeregererface001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> partial interface </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public partial interface IF1
    {
        int Foo();
    }

    public partial interface IF1
    {
        int Bar();
    }

    public class C : IF1
    {
        int IF1.Foo()
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
                result = d.Foo();
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method003a.method003a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.Inheritance.method003a.method003a;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> inheritance </Title>
    // <Description>
    // cast to I's base interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF0
    {
        int Bar();
    }

    public interface IF1 : IF0
    {
        int Foo();
    }

    public class F2
    {
        public virtual int Foo()
        {
            return -1;
        }
    }

    public class C : F2, IF1
    {
        int IF1.Foo()
        {
            return -1;
        }

        public override int Foo()
        {
            return 1;
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
            result = d.Foo() - ((IF1)d).Foo();
            return result - 2;
        }
    }
    // </Code>
}