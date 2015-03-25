// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method001.method001;
    using System;

    public static class Helper
    {
        public static T Cast<T>(dynamic d)
        {
            return (T)d;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method001.method001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method001.method001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // no relative implicitly implemented interface member
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

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method002.method002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method002.method002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // having relative implicitly implemented interface member
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
        int Foo();
        int Bar();
    }

    public struct C : IF1
    {
        int IF1.Foo()
        {
            return 0;
        }

        public int Bar()
        {
            return 1;
        }

        public int Foo()
        {
            return 2;
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
            result = d.Foo();
            if (result != 2)
            {
                error++;
            }

            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method003.method003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method003.method003;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // cast to IF2
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

    public interface IF2
    {
        int Bar();
    }

    public class C : IF1, IF2
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method005.method005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method005.method005;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // cast to IF3
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

    public interface IF3
    {
        int Foo();
    }

    public class C : IF1, IF3
    {
        int IF1.Foo()
        {
            return 0;
        }

        public int Bar()
        {
            return 1;
        }

        int IF3.Foo()
        {
            return 2;
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
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Foo"))
                {
                    error++;
                }
            }

            var x = Helper.Cast<IF3>(d);
            return error;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method006.method006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.method006.method006;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // cast to IF4
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

    public interface IF4
    {
        int Foo();
    }

    public class C : IF1
    {
        int IF1.Foo()
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
            try
            {
                var x = Helper.Cast<IF4>(d);
                System.Console.WriteLine("Should have thrown out exception!");
                error++;
            }
            catch (InvalidCastException ex)
            {
            }

            try
            {
                result = ((IF4)d).Foo();
                System.Console.WriteLine("Should have thrown out runtime exception!");
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property001.property001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property001.property001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // Property : only set clauses in interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Prop
        {
            set;
        }
    }

    public class C : IF1
    {
        public static int Flag = 0;
        int IF1.Prop
        {
            set
            {
                C.Flag = 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int error = 0;
            try
            {
                d.Prop = 1;
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Prop"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property002.property002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property002.property002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // Property : only get clauses in interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Prop
        {
            get;
        }
    }

    public class C : IF1
    {
        public static int Flag = 0;
        int IF1.Prop
        {
            get
            {
                C.Flag = 1;
                return 2;
            }
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
                result = d.Prop;
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Prop"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property003.property003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property003.property003;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // Property : set/get clauses in interface, call set
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Prop
        {
            set;
            get;
        }
    }

    public class C : IF1
    {
        public static int Flag = 0;
        int IF1.Prop
        {
            set
            {
                C.Flag = 3;
            }

            get
            {
                C.Flag = 1;
                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int error = 0;
            try
            {
                d.Prop = 1;
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Prop"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property004.property004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.property004.property004;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // Property : only get clauses in interface
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        int Prop
        {
            get;
            set;
        }
    }

    public class C : IF1
    {
        public static int Flag = 0;
        int IF1.Prop
        {
            set
            {
                C.Flag = 3;
            }

            get
            {
                C.Flag = 1;
                return 2;
            }
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
                result = d.Prop;
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "Prop"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.evnt001.evnt001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.evnt001.evnt001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // event : add /remove clause
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        event Func<int, int> E;
    }

    public class C : IF1
    {
        public static int Flag = 0;
        event Func<int, int> IF1.E
        {
            add
            {
                C.Flag = 1;
            }

            remove
            {
                C.Flag = 3;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int error = 0;
            try
            {
                d.E += (Func<int, int>)(y => y);
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "E"))
                {
                    error++;
                }
            }

            try
            {
                d.E -= (Func<int, int>)(y => y);
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "C", "E"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.indexer001.indexer001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.helper.helper;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.indexer001.indexer001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // indexer : set/get clauses in interface, call set
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        long this[byte index]
        {
            set;
            get;
        }
    }

    public class C : IF1
    {
        public static int Flag = 0;
        long IF1.this[byte index]
        {
            set
            {
                C.Flag = 3;
            }

            get
            {
                C.Flag = 1;
                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int error = 0;
            try
            {
                d[0] = 1; // set
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.BadIndexLHS, ex.Message, "C"))
                {
                    error++;
                }
            }

            try
            {
                var v = d[0]; // get
                System.Console.WriteLine("Should have thrown out runtime exception!");
                error++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.BadIndexLHS, ex.Message, "C"))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.integeregererfacelib001.integeregererfacelib001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.integeregererfacelib001.integeregererfacelib001;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
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
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.integeregererfacelib002.integeregererfacelib002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.ExplicitImple.basic.integeregererfacelib002.integeregererfacelib002;
    // <Area> Dynamic -- explicitly implemented interface member</Area>
    // <Title> Basic usage </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public interface IF1
    {
        event Func<int, int> E;
    }
    // </Code>
}