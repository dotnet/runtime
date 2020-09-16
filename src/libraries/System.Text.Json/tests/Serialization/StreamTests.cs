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

        private static IEnumerable<Type> OuterTypes
            => new[] {
                typeof(Outer<TestClass<NestedClass>, NestedClass>),
                typeof(Outer<TestClass<NestedValueType>, NestedValueType>),
                typeof(Outer<TestValueType<NestedClass>, NestedClass>),
                typeof(Outer<TestValueType<NestedValueType>, NestedValueType>),
                typeof(OuterWithParamCtor),
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public static async Task ContinuationShouldWorkAtAnyPosition(Type outerType, int paddingLength, bool ignoreNullValues)
        {
            var stream = new MemoryStream();
            {
                var outer = (IOuter)Activator.CreateInstance(outerType);
                outer.S = new string('x', paddingLength);
                outer.Initialize();
                
                await JsonSerializer.SerializeAsync(stream, outer, outerType);
            }

            stream.Position = 0;
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 128,
                    IgnoreNullValues = ignoreNullValues,
                };

                var outer = (IOuter)await JsonSerializer.DeserializeAsync(stream, outerType, readOptions);
                Assert.Equal(new string('x', paddingLength), outer.S);
                outer.Verify();
            }
        }

        private static IEnumerable<object[]> TestData()
        {
            foreach (int length in ContinuationPaddingLengths)
            {
                foreach (bool ignore in IgnoreNullValues)
                {
                    foreach (Type outerType in OuterTypes)
                    {
                        yield return new object[] { outerType, length, ignore };
                    }
                }
            }
        }

        private interface IOuter
        {
            string S { get; set; }
            void Initialize();
            void Verify();
        }

        private interface ITestObject
        {
            string A { get; set; }
            string B { get; set; }
            int C { get; set; }
            int? D { get; set; }
            float E { get; set; }
            float? F { get; set; }
            bool G { get; set; }
            bool? H { get; set; }
            int[] I { get; set; }
            INestedObject J { get; set; }
        }

        private interface INestedObject
        {
            string A { get; set; }
            int B { get; set; }
        }

        private class Outer<TTest, TNested> : IOuter where TTest : ITestObject, new() where TNested : INestedObject, new()
        {
            public string S { get; set; }
            public TTest C { get; set; }

            public void Initialize()
            {
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
                    J = new TNested()
                    {
                        A = null,
                        B = 7,
                    },
                };
            }

            public void Verify()
            {
                Assert.Equal("Hello", C.A);
                Assert.Null(C.B);
                Assert.Equal(42, C.C);
                Assert.Null(C.D);
                Assert.Equal(3.14e17f, C.E);
                Assert.Null(C.F);
                Assert.True(C.G);
                Assert.Null(C.H);
                Assert.Collection(C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(C.J);
                Assert.Null(C.J.A);
                Assert.Equal(7, C.J.B);
            }
        }

        private class TestClass<TNested> : ITestObject where TNested : INestedObject
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
            INestedObject ITestObject.J
            {
                get => J;
                set => J = (TNested)value;
            }
        }

        private class TestValueType<TNested> : ITestObject where TNested : INestedObject
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
            INestedObject ITestObject.J
            {
                get => J;
                set => J = (TNested)value;
            }
        }

        private class NestedClass : INestedObject
        {
            public string A { get; set; }
            public int B { get; set; }
        }

        public struct NestedValueType : INestedObject
        {
            public string A { get; set; }
            public int B { get; set; }
        }

        private class OuterWithParamCtor : IOuter
        {
            public string S { get; set; }
            public TestClassWithParamCtor C { get; set; }

            public void Initialize()
            {
                C = new(null, 42, null, 3.14e+17f, null, true, null, new int[] { 42, 17 })
                {
                    A = "Hello",
                    J = new(null)
                    {
                        B = 7,
                    },
                };
            }

            public void Verify()
            {
                Assert.Equal("Hello", C.A);
                Assert.Null(C.B);
                Assert.Equal(42, C.C);
                Assert.Null(C.D);
                Assert.Equal(3.14e17f, C.E);
                Assert.Null(C.F);
                Assert.True(C.G);
                Assert.Null(C.H);
                Assert.Collection(C.I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(C.J);
                Assert.Null(C.J.A);
                Assert.Equal(7, C.J.B);
            }
        }

        private class TestClassWithParamCtor : TestClass<NestedClassWithParamCtor>
        {
            public TestClassWithParamCtor(string b, int c, int? d, float e, float? f, bool g, bool? h, int[] i)
                => (B, C, D, E, F, G, H, I) = (b, c, d, e, f, g, h, i);
        }

        private class NestedClassWithParamCtor : NestedClass
        {
            public NestedClassWithParamCtor(string a)
                => A = a;
        }
    }
}
