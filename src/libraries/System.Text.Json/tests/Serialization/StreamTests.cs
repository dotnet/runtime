// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        // To hit all possible continuation positions inside the tested object,
        // the outer-class padding needs to be between 5 and 116 bytes long.

        // {"S":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","C":{"A":null,"B":"Hello","C":42,"D":null,"E":3.14E+17,"F":null,"G":true,"H":null,"I":[42,17],"J":{"A":null,"B":7}}}
        // |<------------------------------------------------128 byte buffer------------------------------------------------------------->|
        // {"S":"xxxxx","C":{"A":"Hello","B":null,"C":42,"D":null,"E":3.14E+17,"F":null,"G":true,"H":null,"I":[42,17],"J":{"A":null,"B":7}}}

        private const int MinPaddingLength = 5;
        private const int MaxPaddingLength = 116;

        private static IEnumerable<int> ContinuationPaddingLengths
            => Enumerable.Range(MinPaddingLength, MaxPaddingLength - MinPaddingLength + 1);

        private static IEnumerable<bool> IgnoreNullValues
            => new[] { true, false };

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition_Class_Class(int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClass<NestedClass>>
                {
                    S = new string('x', paddingLength),
                    C = new()
                    {
                        A = "Hello",
                        B = null,
                        C = 42,
                        D = null,
                        E = 3.14e+17f,
                        F = null,
                        G = true,
                        H = null,
                        I = new int[] {42, 17},
                        J = new()
                        {
                            A = null,
                            B = 7,
                        }
                    }
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                Outer<TestClass<NestedClass>> obj = await JsonSerializer.DeserializeAsync<Outer<TestClass<NestedClass>>>(stream, readOptions);

                Assert.Equal(new string('x', paddingLength), obj.S);
                Assert.Equal("Hello", obj.C.A);
                Assert.Null(obj.C.B);
                Assert.Equal(42, obj.C.C);
                Assert.Null(obj.C.D);
                Assert.Equal(3.14e17f, obj.C.E);
                Assert.Null(obj.C.F);
                Assert.True(obj.C.G);
                Assert.Null(obj.C.H);
                Assert.Collection(obj.C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(obj.C.J);
                Assert.Null(obj.C.J.A);
                Assert.Equal(7, obj.C.J.B);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition_Class_ValueType(int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClass<NestedValueType>>
                {
                    S = new string('x', paddingLength),
                    C = new()
                    {
                        A = "Hello",
                        B = null,
                        C = 42,
                        D = null,
                        E = 3.14e+17f,
                        F = null,
                        G = true,
                        H = null,
                        I = new int[] { 42, 17 },
                        J = new()
                        {
                            A = null,
                            B = 7,
                        }
                    }
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                Outer<TestClass<NestedValueType>> obj = await JsonSerializer.DeserializeAsync<Outer<TestClass<NestedValueType>>>(stream, readOptions);

                Assert.Equal(new string('x', paddingLength), obj.S);
                Assert.Equal("Hello", obj.C.A);
                Assert.Null(obj.C.B);
                Assert.Equal(42, obj.C.C);
                Assert.Null(obj.C.D);
                Assert.Equal(3.14e17f, obj.C.E);
                Assert.Null(obj.C.F);
                Assert.True(obj.C.G);
                Assert.Null(obj.C.H);
                Assert.Collection(obj.C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.Null(obj.C.J.A);
                Assert.Equal(7, obj.C.J.B);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ValueType_Class(int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestValueType<NestedClass>>
                {
                    S = new string('x', paddingLength),
                    C = new()
                    {
                        A = "Hello",
                        B = null,
                        C = 42,
                        D = null,
                        E = 3.14e+17f,
                        F = null,
                        G = true,
                        H = null,
                        I = new int[] { 42, 17 },
                        J = new()
                        {
                            A = null,
                            B = 7,
                        }
                    }
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                Outer<TestValueType<NestedClass>> obj = await JsonSerializer.DeserializeAsync<Outer<TestValueType<NestedClass>>>(stream, readOptions);

                Assert.Equal(new string('x', paddingLength), obj.S);
                Assert.Equal("Hello", obj.C.A);
                Assert.Null(obj.C.B);
                Assert.Equal(42, obj.C.C);
                Assert.Null(obj.C.D);
                Assert.Equal(3.14e17f, obj.C.E);
                Assert.Null(obj.C.F);
                Assert.True(obj.C.G);
                Assert.Null(obj.C.H);
                Assert.Collection(obj.C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(obj.C.J);
                Assert.Null(obj.C.J.A);
                Assert.Equal(7, obj.C.J.B);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ValueType_ValueType(int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestValueType<NestedValueType>>
                {
                    S = new string('x', paddingLength),
                    C = new()
                    {
                        A = "Hello",
                        B = null,
                        C = 42,
                        D = null,
                        E = 3.14e+17f,
                        F = null,
                        G = true,
                        H = null,
                        I = new int[] { 42, 17 },
                        J = new()
                        {
                            A = null,
                            B = 7,
                        }
                    }
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                Outer<TestValueType<NestedValueType>> obj = await JsonSerializer.DeserializeAsync<Outer<TestValueType<NestedValueType>>>(stream, readOptions);

                Assert.Equal(new string('x', paddingLength), obj.S);
                Assert.Equal("Hello", obj.C.A);
                Assert.Null(obj.C.B);
                Assert.Equal(42, obj.C.C);
                Assert.Null(obj.C.D);
                Assert.Equal(3.14e17f, obj.C.E);
                Assert.Null(obj.C.F);
                Assert.True(obj.C.G);
                Assert.Null(obj.C.H);
                Assert.Collection(obj.C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.Null(obj.C.J.A);
                Assert.Equal(7, obj.C.J.B);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ClassWithParamCtor_Class(int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClassWithParamCtor<NestedClassWithParamCtor>>
                {
                    S = new string('x', paddingLength),
                    C = new(null, 42, null, 3.14e+17f, null, true, null, new int[] { 42, 17 })
                    {
                        A = "Hello",
                        J = new(null)
                        {
                            B = 7,
                        },
                    },
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                Outer<TestClassWithParamCtor<NestedClassWithParamCtor>> obj = await JsonSerializer.DeserializeAsync<Outer<TestClassWithParamCtor<NestedClassWithParamCtor>>>(stream, readOptions);

                Assert.Equal(new string('x', paddingLength), obj.S);
                Assert.Equal("Hello", obj.C.A);
                Assert.Null(obj.C.B);
                Assert.Equal(42, obj.C.C);
                Assert.Null(obj.C.D);
                Assert.Equal(3.14e17f, obj.C.E);
                Assert.Null(obj.C.F);
                Assert.True(obj.C.G);
                Assert.Null(obj.C.H);
                Assert.Collection(obj.C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.Null(obj.C.J.A);
                Assert.Equal(7, obj.C.J.B);
            }
        }

        private static IEnumerable<object[]> TestData()
        {
            foreach (int length in ContinuationPaddingLengths)
            {
                foreach (bool ignore in IgnoreNullValues)
                {
                    yield return new object[] { length, ignore };
                }
            }
        }

        private class Outer<TTest>
        {
            public string S { get; set; }
            public TTest C { get; set; }
        }

        private class TestClass<TNested>
        {
            public string A { get; set; }
            public string B { get; set; }
            public int C { get; set; }
            public int? D { get; set; }
            public float E { get; set; }
            public float? F { get; set; }
            public bool G { get; set; }
            public bool? H { get; set; }
            public int[] I { get; set; }
            public TNested J { get; set; }
        }

        private class TestClassWithParamCtor<TNested> : TestClass<TNested>
        {
            public TestClassWithParamCtor(string b, int c, int? d, float e, float? f, bool g, bool? h, int[] i)
                => (B, C, D, E, F, G, H, I) = (b, c, d, e, f, g, h, i);
        }

        private class TestValueType<TNested>
        {
            public string A { get; set; }
            public string B { get; set; }
            public int C { get; set; }
            public int? D { get; set; }
            public float E { get; set; }
            public float? F { get; set; }
            public bool G { get; set; }
            public bool? H { get; set; }
            public int[] I { get; set; }
            public TNested J { get; set; }
        }

        private class NestedClass
        {
            public string A { get; set; }
            public int B { get; set; }
        }

        private class NestedClassWithParamCtor : NestedClass
        {
            public NestedClassWithParamCtor(string a)
                => A = a;
        }

        public struct NestedValueType
        {
            public string A { get; set; }
            public int B { get; set; }
        }
    }
}
