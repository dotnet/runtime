// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.bol.bol
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.bol.bol;
    // <Title> Generated tests for <<= operator bool Type.</Title>
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
                    d <<= d0;
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
                    d <<= d1;
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
                    d <<= d2;
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
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
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
                    d <<= d6;
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
                    d <<= d7;
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
                    d <<= d8;
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
                    d <<= d9;
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
                    d <<= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "bool", "short"))
                        result = false;
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
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
                    d <<= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.bte.bte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.bte.bte;
    // <Title> Generated tests for <<= operator byte Type.</Title>
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
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d1;
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d2;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d6;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d9;
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d10;
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "byte", "uint"))
                        result = false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.chr.chr
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.chr.chr;
    // <Title> Generated tests for <<= operator char Type.</Title>
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
                char a = 'a';
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "char", "bool"))
                        result = false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d1;
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d2;
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d6;
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d <<= d7;
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
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d9;
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d10;
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.dcml.dcml
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.dcml.dcml;
    // <Title> Generated tests for <<= operator decimal Type.</Title>
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
                    d <<= d0;
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
                    d <<= d1;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "decimal", "byte"))
                        result = false;
                }
            }

            {
                decimal a = 1M;
                dynamic d = a;
                try
                {
                    d <<= d2;
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
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
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
                    d <<= d6;
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
                    d <<= d7;
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
                    d <<= d8;
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
                    d <<= d9;
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
                    d <<= d10;
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
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
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
                    d <<= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.dbl.dbl
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.dbl.dbl;
    // <Title> Generated tests for <<= operator double Type.</Title>
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
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d1;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d2;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "double", "char"))
                        result = false;
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d6;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10.1;
                dynamic d = a;
                try
                {
                    d <<= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.flt.flt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.flt.flt;
    // <Title> Generated tests for <<= operator float Type.</Title>
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
                    d <<= d0;
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
                    d <<= d1;
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
                    d <<= d2;
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
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "float", "decimal"))
                        result = false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d <<= d4;
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
                    d <<= d5;
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
                    d <<= d6;
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
                    d <<= d7;
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
                    d <<= d8;
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
                    d <<= d9;
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
                    d <<= d10;
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
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
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
                    d <<= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.integereger.integereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.integereger.integereger;
    // <Title> Generated tests for <<= operator int Type.</Title>
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
                int a = 10;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d1;
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d2;
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d6;
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d <<= d7;
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
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d9;
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d10;
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "int", "string"))
                        result = false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d <<= d12;
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
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                int a = 10;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.lng.lng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.lng.lng;
    // <Title> Generated tests for <<= operator long Type.</Title>
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
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "long", "bool"))
                        result = false;
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d1;
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d2;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d6;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d9;
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d10;
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 5;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.obj.obj
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.obj.obj;
    // <Title> Generated tests for <<= operator object Type.</Title>
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
                    d <<= d0;
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
                    d <<= d1;
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
                    d <<= d2;
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
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
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
                    d <<= d6;
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
                    d <<= d7;
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
                    d <<= d8;
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
                    d <<= d9;
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
                    d <<= d10;
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
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
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
                    d <<= d14;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "object", "ushort"))
                        result = false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.sbte.sbte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.sbte.sbte;
    // <Title> Generated tests for <<= operator sbyte Type.</Title>
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
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d1;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d2;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d6;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d <<= d7;
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
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d9;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d10;
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d <<= d11;
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
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "sbyte", "uint"))
                        result = false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.shrt.shrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.shrt.shrt;
    // <Title> Generated tests for <<= operator short Type.</Title>
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
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d1;
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d2;
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d6;
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d9;
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d10;
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "short", "string"))
                        result = false;
                }
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                short a = 6;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.str.str
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.str.str;
    // <Title> Generated tests for <<= operator string Type.</Title>
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
                    d <<= d0;
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
                    d <<= d1;
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
                    d <<= d2;
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
                    d <<= d3;
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
                    d <<= d4;
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
                    d <<= d5;
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
                    d <<= d6;
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
                    d <<= d7;
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
                    d <<= d8;
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
                    d <<= d9;
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
                    d <<= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "string", "short"))
                        result = false;
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d <<= d11;
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
                    d <<= d12;
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
                    d <<= d13;
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
                    d <<= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.uintegereger.uintegereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.uintegereger.uintegereger;
    // <Title> Generated tests for <<= operator uint Type.</Title>
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
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d1;
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d2;
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d6;
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "uint", "object"))
                        result = false;
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d9;
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d10;
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                uint a = 15;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.ulng.ulng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.ulng.ulng;
    // <Title> Generated tests for <<= operator ulong Type.</Title>
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
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d1;
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d2;
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d6;
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "ulong", "long"))
                        result = false;
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d9;
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d10;
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ulong a = 2;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.ushrt.ushrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.type.ShiftLeftEqual.ushrt.ushrt;
    // <Title> Generated tests for <<= operator ushort Type.</Title>
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
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d1;
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d2;
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "<<=", "ushort", "double"))
                        result = false;
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d6;
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d9;
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d10;
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                try
                {
                    d <<= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                ushort a = 7;
                dynamic d = a;
                d <<= d14;
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}