// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_dcml001.nullandnonnullableadd_dcml001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_dbl001.nullandnonnullableadd_dbl001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_enum001.nullandnonnullableadd_enum001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_flt001.nullandnonnullableadd_flt001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_integereger001.nullandnonnullableadd_integereger001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_lng001.nullandnonnullableadd_lng001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_uintegereger001.nullandnonnullableadd_uintegereger001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableadd_ulng001.nullandnonnullableadd_ulng001
{
    // <Title>Need to warn on build-in addition operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a + null;
                if (test0 == null)
                    rez++;
                var test1 = null + a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_bol001.nullandnonnullableand_bol001
{
    // <Title>DO NOT warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(28,26\).*CS0458</Expects>
    //<Expects Status=warning>\(29,30\).*CS0458</Expects>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = true;
                if ((a & null) == null)
                    if ((null & a) == null)
                        if ((true & null) == null)
                            if ((null & true) == null)
                                return 0;
                return rez == 0 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_enum001.nullandnonnullableand_enum001
{
    // <Title>Need to warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a & null;
                if (test0 == null)
                    rez++;
                var test1 = null & a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_integereger001.nullandnonnullableand_integereger001
{
    // <Title>Need to warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a & null;
                if (test0 == null)
                    rez++;
                var test1 = null & a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_lng001.nullandnonnullableand_lng001
{
    // <Title>Need to warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a & null;
                if (test0 == null)
                    rez++;
                var test1 = null & a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_uintegereger001.nullandnonnullableand_uintegereger001
{
    // <Title>Need to warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a & null;
                if (test0 == null)
                    rez++;
                var test1 = null & a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableand_ulng001.nullandnonnullableand_ulng001
{
    // <Title>Need to warn on build-in bitwise logical AND operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a & null;
                if (test0 == null)
                    rez++;
                var test1 = null & a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_dcml001.nullandnonnullablediv_dcml001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_dbl001.nullandnonnullablediv_dbl001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_flt001.nullandnonnullablediv_flt001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_integereger001.nullandnonnullablediv_integereger001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_lng001.nullandnonnullablediv_lng001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_uintegereger001.nullandnonnullablediv_uintegereger001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablediv_ulng001.nullandnonnullablediv_ulng001
{
    // <Title>Need to warn on build-in division operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a / null;
                if (test0 == null)
                    rez++;
                var test1 = null / a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_bol001.nullandnonnullableeq_bol001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = true;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_dcml001.nullandnonnullableeq_dcml001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_dbl001.nullandnonnullableeq_dbl001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_enum001.nullandnonnullableeq_enum001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_flt001.nullandnonnullableeq_flt001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_integereger001.nullandnonnullableeq_integereger001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_lng001.nullandnonnullableeq_lng001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_uintegereger001.nullandnonnullableeq_uintegereger001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableeq_ulng001.nullandnonnullableeq_ulng001
{
    // <Title>Need to warn on build-in equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a == null;
                if (test0 == false)
                    rez++;
                var test1 = null == a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_dcml001.nullandnonnullablege_dcml001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_dbl001.nullandnonnullablege_dbl001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_enum001.nullandnonnullablege_enum001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_flt001.nullandnonnullablege_flt001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_integereger001.nullandnonnullablege_integereger001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_lng001.nullandnonnullablege_lng001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_uintegereger001.nullandnonnullablege_uintegereger001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablege_ulng001.nullandnonnullablege_ulng001
{
    // <Title>Need to warn on build-in greater than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a >= null;
                if (test0 == false)
                    rez++;
                var test1 = null >= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_dcml001.nullandnonnullablegt_dcml001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_dbl001.nullandnonnullablegt_dbl001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_enum001.nullandnonnullablegt_enum001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_flt001.nullandnonnullablegt_flt001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_integereger001.nullandnonnullablegt_integereger001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_lng001.nullandnonnullablegt_lng001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_uintegereger001.nullandnonnullablegt_uintegereger001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablegt_ulng001.nullandnonnullablegt_ulng001
{
    // <Title>Need to warn on build-in greater than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a > null;
                if (test0 == false)
                    rez++;
                var test1 = null > a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_dcml001.nullandnonnullablele_dcml001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_dbl001.nullandnonnullablele_dbl001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_enum001.nullandnonnullablele_enum001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_flt001.nullandnonnullablele_flt001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_integereger001.nullandnonnullablele_integereger001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_lng001.nullandnonnullablele_lng001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_uintegereger001.nullandnonnullablele_uintegereger001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablele_ulng001.nullandnonnullablele_ulng001
{
    // <Title>Need to warn on build-in less than or equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a <= null;
                if (test0 == false)
                    rez++;
                var test1 = null <= a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_dcml001.nullandnonnullablelt_dcml001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_dbl001.nullandnonnullablelt_dbl001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_enum001.nullandnonnullablelt_enum001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_flt001.nullandnonnullablelt_flt001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_integereger001.nullandnonnullablelt_integereger001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_lng001.nullandnonnullablelt_lng001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_uintegereger001.nullandnonnullablelt_uintegereger001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablelt_ulng001.nullandnonnullablelt_ulng001
{
    // <Title>Need to warn on build-in less than comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a < null;
                if (test0 == false)
                    rez++;
                var test1 = null < a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_dcml001.nullandnonnullablemul_dcml001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_dbl001.nullandnonnullablemul_dbl001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_flt001.nullandnonnullablemul_flt001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_integereger001.nullandnonnullablemul_integereger001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_lng001.nullandnonnullablemul_lng001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_uintegereger001.nullandnonnullablemul_uintegereger001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablemul_ulng001.nullandnonnullablemul_ulng001
{
    // <Title>Need to warn on build-in multiplication operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a * null;
                if (test0 == null)
                    rez++;
                var test1 = null * a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_bol001.nullandnonnullableneq_bol001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = true;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_dcml001.nullandnonnullableneq_dcml001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_dbl001.nullandnonnullableneq_dbl001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a != null;
                if (test0 != false)
                    rez++;
                var test1 = null != a;
                if (test1 != false)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_enum001.nullandnonnullableneq_enum001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_flt001.nullandnonnullableneq_flt001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a != null;
                if (test0 == false)
                    rez++;
                var test1 = null != a;
                if (test1 == false)
                    rez++;
                return rez == 2 ? 1 : 0;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_integereger001.nullandnonnullableneq_integereger001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_lng001.nullandnonnullableneq_lng001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_uintegereger001.nullandnonnullableneq_uintegereger001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableneq_ulng001.nullandnonnullableneq_ulng001
{
    // <Title>Need to warn on build-in not equal comparison operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a != null;
                if (test0 == true)
                    rez++;
                var test1 = null != a;
                if (test1 == true)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_bol001.nullandnonnullableor_bol001
{
    // <Title>DO NOT warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(28,26\).*CS0458</Expects>
    //<Expects Status=warning>\(29,30\).*CS0458</Expects>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = false;
                if ((a | null) == null)
                    if ((null | a) == null)
                        if ((false | null) == null)
                            if ((null | false) == null)
                                return 0;
                return rez == 0 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_enum001.nullandnonnullableor_enum001
{
    // <Title>Need to warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a | null;
                if (test0 == null)
                    rez++;
                var test1 = null | a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_integereger001.nullandnonnullableor_integereger001
{
    // <Title>Need to warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a | null;
                if (test0 == null)
                    rez++;
                var test1 = null | a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_lng001.nullandnonnullableor_lng001
{
    // <Title>Need to warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a | null;
                if (test0 == null)
                    rez++;
                var test1 = null | a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_uintegereger001.nullandnonnullableor_uintegereger001
{
    // <Title>Need to warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a | null;
                if (test0 == null)
                    rez++;
                var test1 = null | a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableor_ulng001.nullandnonnullableor_ulng001
{
    // <Title>Need to warn on build-in bitwise logical OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a | null;
                if (test0 == null)
                    rez++;
                var test1 = null | a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_dcml001.nullandnonnullablerem_dcml001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_dbl001.nullandnonnullablerem_dbl001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_flt001.nullandnonnullablerem_flt001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_integereger001.nullandnonnullablerem_integereger001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_lng001.nullandnonnullablerem_lng001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_uintegereger001.nullandnonnullablerem_uintegereger001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablerem_ulng001.nullandnonnullablerem_ulng001
{
    // <Title>Need to warn on build-in remainder operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a % null;
                if (test0 == null)
                    rez++;
                var test1 = null % a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshl_integereger001.nullandnonnullableshl_integereger001
{
    // <Title>Need to warn on build-in shift left operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a << null;
                if (test0 == null)
                    rez++;
                var test1 = null << a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshl_lng001.nullandnonnullableshl_lng001
{
    // <Title>Need to warn on build-in shift left operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a << null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshl_uintegereger001.nullandnonnullableshl_uintegereger001
{
    // <Title>Need to warn on build-in shift left operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a << null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshl_ulng001.nullandnonnullableshl_ulng001
{
    // <Title>Need to warn on build-in shift left operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a << null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshr_integereger001.nullandnonnullableshr_integereger001
{
    // <Title>Need to warn on build-in shift right operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a >> null;
                if (test0 == null)
                    rez++;
                var test1 = null >> a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshr_lng001.nullandnonnullableshr_lng001
{
    // <Title>Need to warn on build-in shift right operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a >> null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshr_uintegereger001.nullandnonnullableshr_uintegereger001
{
    // <Title>Need to warn on build-in shift right operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a >> null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullableshr_ulng001.nullandnonnullableshr_ulng001
{
    // <Title>Need to warn on build-in shift right operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a >> null;
                if (test0 == null)
                    rez++;
                return rez == 1 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_dcml001.nullandnonnullablesub_dcml001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5M;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_dbl001.nullandnonnullablesub_dbl001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0D;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_enum001.nullandnonnullablesub_enum001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = Colors.red;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_flt001.nullandnonnullablesub_flt001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5.0F;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_integereger001.nullandnonnullablesub_integereger001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_lng001.nullandnonnullablesub_lng001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_uintegereger001.nullandnonnullablesub_uintegereger001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablesub_ulng001.nullandnonnullablesub_ulng001
{
    // <Title>Need to warn on build-in subtraction operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a - null;
                if (test0 == null)
                    rez++;
                var test1 = null - a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_bol001.nullandnonnullablexor_bol001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = true;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_enum001.nullandnonnullablexor_enum001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = (ulong)Colors.red;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }

            public enum Colors
            {
                red,
                green,
                blue
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_integereger001.nullandnonnullablexor_integereger001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_lng001.nullandnonnullablexor_lng001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5L;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_uintegereger001.nullandnonnullablexor_uintegereger001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5U;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullandnonnullablexor_ulng001.nullandnonnullablexor_ulng001
{
    // <Title>Need to warn on build-in bitwise logical exclusive OR operator if one op is null and the other is non-nullable.</Title>
    // <Description>
    // We should warn whenever one of the builtin nullable operators is used and one op is null and
    // the other is not a nullable
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                int rez = 0;
                dynamic a = 5UL;
                var test0 = a ^ null;
                if (test0 == null)
                    rez++;
                var test1 = null ^ a;
                if (test1 == null)
                    rez++;
                return rez == 2 ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.verify.verify
{
    public class Verify
    {
        public static int Check(dynamic actual, dynamic expected)
        {
            int index = 0;
            foreach (var item in expected)
            {
                if (actual[index] != expected[index])
                    return 0;
                index++;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullcoalescing004.nullcoalescing004
{
    // <Title>dynamic and nullable</Title>
    // <Description>
    // null-coalescing operator
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        One = 1,
        Two = 2
    }

    public struct S
    {
    }

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                E? x = E.One, y = null;
                dynamic d1 = new S();
                dynamic d2 = null;
                dynamic[] result = new dynamic[4];
                dynamic[] expected = new dynamic[]
                {
                E.One, null, new S(), new S()}

                ;
                result[0] = x ?? d1;
                result[1] = y ?? d2;
                result[2] = d1 ?? x;
                result[3] = d1 ?? y;
                if (result[0] == expected[0] && result[1] == expected[1] && result[2].GetType() == expected[2].GetType() && result[3].GetType() == expected[3].GetType())
                    return 0;
                return 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.nullcoalescing005.nullcoalescing005
{
    // <Title>dynamic and nullable</Title>
    // <Description>
    // null-coalescing operator
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    //
    // <Code>

    public enum E
    {
        One = 1,
        Two = 2
    }

    public struct S
    {
    }

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                S? x = new S(), y = null;
                dynamic d1 = E.One;
                dynamic d2 = null;
                dynamic[] result = new dynamic[4];
                dynamic[] expected = new dynamic[]
                {
                new S(), null, E.One, E.One
                }

                ;
                result[0] = x ?? d1;
                result[1] = y ?? d2;
                result[2] = d1 ?? x;
                result[3] = d1 ?? y;
                if (result[0].GetType() == expected[0].GetType() && result[1] == expected[1] && result[2] == expected[2] && result[3] == expected[3])
                    return 0;
                return 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.conversion001.conversion001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.nullable.verify.verify;
    // <Title>dynamic and nullable</Title>
    // <Description>
    // conversion
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    //
    // <Code>

    namespace Test
    {
        public class Program
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                dynamic d1 = null;
                dynamic d2 = default(dynamic);
                int? i1 = d1, i2 = d2;
                char? c1 = d1, c2 = d2;
                decimal? m1 = d1, m2 = d2;
                E? e1 = d1, e2 = d2;
                S? s1 = d1, s2 = d2;
                dynamic result = new dynamic[]
                {
                i1, i2, c1, c2, m1, m2, e1, e2, s1, s2
                }

                ;
                dynamic expected = new dynamic[]
                {
                null, null, null, null, null, null, null, null, null, null
                }

                ;
                return Verify.Check(result, expected);
            }
        }

        public enum E
        {
        }

        public struct S
        {
        }
    }
    // </Code>
}
