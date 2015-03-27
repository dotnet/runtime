// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.bol.bol
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.bol.bol;
    // <Title> Generated tests for %= operator bool Type.</Title>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d %= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "bool", "bool?"))
                        result = false;
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d %= d1;
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
                    d %= d2;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "bool", "char?"))
                        result = false;
                }
            }

            {
                bool a = true;
                dynamic d = a;
                try
                {
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
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
                    d %= d12;
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
                    d %= d13;
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
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.bte.bte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.bte.bte;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "byte", "string"))
                        result = false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                byte a = 1;
                dynamic d = a;
                try
                {
                    d %= d13;
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
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.chr.chr
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.chr.chr;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "char", "string"))
                        result = false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                char a = 'a';
                dynamic d = a;
                try
                {
                    d %= d13;
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
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.dcml.dcml
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.dcml.dcml;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d4;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d5;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d9;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "decimal", "string"))
                        result = false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                decimal a = 10;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.dbl.dbl
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.dbl.dbl;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d9;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "double", "string"))
                        result = false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                double a = 10;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.flt.flt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.flt.flt;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d %= d2;
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
                try
                {
                    d %= d3;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d %= d4;
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
                try
                {
                    d %= d5;
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
                try
                {
                    d %= d6;
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
                try
                {
                    d %= d7;
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
                try
                {
                    d %= d8;
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
                    d %= d9;
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
                try
                {
                    d %= d10;
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
                try
                {
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "float", "string"))
                        result = false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                float a = 10.1f;
                dynamic d = a;
                try
                {
                    d %= d13;
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
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.integereger.integereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.integereger.integereger;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "int", "string"))
                        result = false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                int a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
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
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.lng.lng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.lng.lng;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d0;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d6;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d7;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d8;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d9;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d10;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "long", "string"))
                        result = false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                long a = 10L;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.obj.obj
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.obj.obj;
    // <Title> Generated tests for %= operator object Type.</Title>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
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
                    d %= d12;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "object", "uint?"))
                        result = false;
                }
            }

            {
                object a = new object();
                dynamic d = a;
                try
                {
                    d %= d13;
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
                    d %= d14;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "object", "ushort?"))
                        result = false;
                }
            }

            return result ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.sbte.sbte
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.sbte.sbte;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "sbyte", "string"))
                        result = false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                sbyte a = 10;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.shrt.shrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.shrt.shrt;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "short", "string"))
                        result = false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                short a = 10;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.str.str
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.str.str;
    // <Title> Generated tests for %= operator string Type.</Title>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "string", "sbyte?"))
                        result = false;
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "string", "string"))
                        result = false;
                }
            }

            {
                string a = "a";
                dynamic d = a;
                try
                {
                    d %= d12;
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
                    d %= d13;
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
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.uintegereger.uintegereger
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.uintegereger.uintegereger;
    // <Title> Generated tests for /= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d /= d0;
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
                    d /= d1;
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
                    d /= d2;
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
                    d /= d3;
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
                    d /= d4;
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
                    d /= d5;
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
                    d /= d6;
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
                    d /= d7;
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
                    d /= d8;
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
                    d /= d9;
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
                    d /= d10;
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
                    d /= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "/=", "uint", "string"))
                        result = false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d /= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                uint a = 10;
                dynamic d = a;
                try
                {
                    d /= d13;
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
                    d /= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.ulng.ulng
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.ulng.ulng;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
                    result &= false;
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
                    d %= d1;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d2;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d3;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d4;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d5;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d6;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d7;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d8;
                    result &= false;
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
                    d %= d9;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d10;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    //result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "ulong", "string"))
                        result = false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
                    //result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // same error as others, no need to verify
                {
                    result &= false;
                }
            }

            {
                ulong a = 10;
                dynamic d = a;
                try
                {
                    d %= d14;
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.ushrt.ushrt
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.lift.ModEqual.ushrt.ushrt;
    // <Title> Generated tests for %= operator byte Type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(207,74\).*CS0168</Expects>
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
            bool? d0 = null;
            byte? d1 = null;
            char? d2 = null;
            decimal? d3 = null;
            double? d4 = null;
            float? d5 = null;
            int? d6 = null;
            long? d7 = null;
            object d8 = new object();
            sbyte? d9 = null;
            short? d10 = 6;
            string d11 = "a";
            uint? d12 = null;
            ulong? d13 = 2;
            ushort? d14 = 7;
            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d %= d0;
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
                    d %= d1;
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
                    d %= d2;
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
                    d %= d3;
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
                    d %= d4;
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
                    d %= d5;
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
                    d %= d6;
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
                    d %= d7;
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
                    d %= d8;
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
                    d %= d9;
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
                    d %= d10;
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
                    d %= d11;
                    result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (!ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "%=", "ushort", "string"))
                        result = false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d %= d12;
                    // result &= false;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "uint?", "byte"))
                    //    result = false;
                    result &= false;
                }
            }

            {
                ushort a = 10;
                dynamic d = a;
                try
                {
                    d %= d13;
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
                    d %= d14;
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