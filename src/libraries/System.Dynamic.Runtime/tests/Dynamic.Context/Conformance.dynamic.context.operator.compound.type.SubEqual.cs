// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.bol.bol
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.bol.bol;
    // <Title> Generated tests for -= operator bool Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "bool", "bool"))
                        result = false;
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d1;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d2;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d6;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d -= d14;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.bte.bte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.bte.bte;
    // <Title> Generated tests for -= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d1;
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d2;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d6;
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d7;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d9;
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d10;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d12;
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d13;
            }

            {
                byte a = 1;
                dynamic d = a;
                d -= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.chr.chr
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.chr.chr;
    // <Title> Generated tests for -= operator char Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(73,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                d -= d2;
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result = false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "char", "decimal"))
                    //    result = false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d6;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d7;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d9;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d10;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d12;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d -= d14;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.dcml.dcml
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.dcml.dcml;
    // <Title> Generated tests for -= operator decimal Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d1;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d2;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d3;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d -= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d -= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "decimal", "float"))
                        result = false;
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d6;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d7;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d9;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d10;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d12;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d13;
            }

            {
                decimal a = 1M;
                dynamic d = a;
                d -= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.dbl.dbl
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.dbl.dbl;
    // <Title> Generated tests for -= operator double Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d1;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d2;
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d -= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "double", "decimal"))
                        result = false;
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d4;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d5;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d6;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d7;
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d9;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d10;
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d12;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d13;
            }

            {
                double a = 10.1;
                dynamic d = a;
                d -= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.flt.flt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.flt.flt;
    // <Title> Generated tests for -= operator float Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d1;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d2;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d -= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d -= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d5;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d6;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d7;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d9;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d10;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "float", "string"))
                        result = false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d12;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d13;
            }

            {
                float a = 10.1f;
                dynamic d = a;
                d -= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.integereger.integereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.integereger.integereger;
    // <Title> Generated tests for -= operator int Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(73,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                d -= d2;
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result &= false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "int", "decimal"))
                    //    result = false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d9;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            // ulong 's behavior is not same as others
            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.lng.lng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.lng.lng;
    // <Title> Generated tests for -= operator long Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d1;
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d2;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d6;
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d7;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "long", "object"))
                        result = false;
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d9;
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d10;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d12;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d -= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.obj.obj
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.obj.obj;
    // <Title> Generated tests for -= operator object Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d1;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d2;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d6;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "object", "int"))
                        result = false;
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d -= d14;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.sbte.sbte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.sbte.sbte;
    // <Title> Generated tests for -= operator sbyte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(110,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result &= false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "sbyte"))
                    //    result = false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d -= d9;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.shrt.shrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.shrt.shrt;
    // <Title> Generated tests for -= operator sbyte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(110,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result &= false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "sbyte"))
                    //    result = false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 10;
                dynamic d = a;
                d -= d9;
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.str.str
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.str.str;
    // <Title> Generated tests for -= operator string Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d1;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d2;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d6;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "string", "int"))
                        result = false;
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d -= d14;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.uintegereger.uintegereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.uintegereger.uintegereger;
    // <Title> Generated tests for -= operator sbyte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(110,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result &= false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "sbyte"))
                    //    result = false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                d -= d9;
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.ulng.ulng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.ulng.ulng;
    // <Title> Generated tests for -= operator sbyte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(110,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d2;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    // result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result = false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "sbyte"))
                    //    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d9;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "-=", "ulong", "sbyte"))
                        result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                    //result = false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result = false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.ushrt.ushrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.SubEqual.ushrt.ushrt;
    // <Title> Generated tests for -= operator sbyte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(110,74\).*CS0168</Expects>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            bool result = true;
            bool d0 = true;
            byte d1 = 1;
            char d2 = 'a';
            decimal d3 = 1M;
            double d4 = 10.1;
            float d5 = 10.1f;
            int d6 = 10;
            long d7 = 5;
            object d8 = new object();
            sbyte d9 = 10;
            short d10 = 6;
            string d11 = "a";
            uint d12 = 15;
            ulong d13 = 2;
            ushort d14 = 7;
            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d1;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d4;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    result &= false;
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "sbyte"))
                    //    result = false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                d -= d9;
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d12;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d -= d14;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}