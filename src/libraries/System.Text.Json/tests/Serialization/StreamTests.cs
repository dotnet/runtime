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
        // the outer-class spacer needs to be between 5 and 116 bytes long.

        // {"S":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","C":{"A":null,"B":"Hello","C":42,"D":null,"E":3.14E+17,"F":null,"G":true,"H":null,"I":[42,17],"J":{"A":null,"B":7}}}
        // |<------------------------------------------------128 byte buffer------------------------------------------------------------->|
        // {"S":"xxxxx","C":{"A":"Hello","B":null,"C":42,"D":null,"E":3.14E+17,"F":null,"G":true,"H":null,"I":[42,17],"J":{"A":null,"B":7}}}
        public static IEnumerable<object[]> ContinuationSpacerLengths
            => Enumerable.Range(5, 116 - 5 + 1).Select(length => new object[] { length });

        [Theory]
        [MemberData(nameof(ContinuationSpacerLengths))]
        public static async Task ContinuationShouldWorkAtAnyPosition_Class_Class(int spacerLength)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClass<NestedClass>>
                {
                    S = new string('x', spacerLength),
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
                var json = Encoding.UTF8.GetString(stream.ToArray()); 
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<Outer<TestClass<NestedClass>>>(stream, readOptions);

                Assert.Equal(new string('x', spacerLength), obj.S);
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
        [MemberData(nameof(ContinuationSpacerLengths))]
        public static async Task ContinuationShouldWorkAtAnyPosition_Class_ValueType(int spacerLength)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClass<NestedValueType>>
                {
                    S = new string('x', spacerLength),
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
                var json = Encoding.UTF8.GetString(stream.ToArray());
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<Outer<TestClass<NestedValueType>>>(stream, readOptions);

                Assert.Equal(new string('x', spacerLength), obj.S);
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
        [MemberData(nameof(ContinuationSpacerLengths))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ValueType_Class(int spacerLength)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestValueType<NestedClass>>
                {
                    S = new string('x', spacerLength),
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
                var json = Encoding.UTF8.GetString(stream.ToArray());
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<Outer<TestValueType<NestedClass>>>(stream, readOptions);

                Assert.Equal(new string('x', spacerLength), obj.S);
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
        [MemberData(nameof(ContinuationSpacerLengths))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ValueType_ValueType(int spacerLength)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestValueType<NestedValueType>>
                {
                    S = new string('x', spacerLength),
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
                var json = Encoding.UTF8.GetString(stream.ToArray());
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<Outer<TestValueType<NestedValueType>>>(stream, readOptions);

                Assert.Equal(new string('x', spacerLength), obj.S);
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
        [MemberData(nameof(ContinuationSpacerLengths))]
        public static async Task ContinuationShouldWorkAtAnyPosition_ClassWithParamCtor_Class(int spacerLength)
        {
            var stream = new MemoryStream();
            {
                var obj = new Outer<TestClassWithParamCtor<NestedClassWithParamCtor>>
                {
                    S = new string('x', spacerLength),
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
                var json = Encoding.UTF8.GetString(stream.ToArray());
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<Outer<TestClassWithParamCtor<NestedClassWithParamCtor>>>(stream, readOptions);

                Assert.Equal(new string('x', spacerLength), obj.S);
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
