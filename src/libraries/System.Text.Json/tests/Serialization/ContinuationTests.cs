// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ContinuationTests
    {
        private static readonly Func<string, string>[] s_payloadTweaks = new Func<string, string>[]
        {
            payload => payload,
            payload => payload.Replace("null", "nullX"),
            payload => payload.Replace("e+17", "e+-17"),
        };

        private static IEnumerable<(ITestObject, INestedObject)> TestObjects
            => new (ITestObject, INestedObject)[]
            {
                (new TestClass<NestedClass>(), new NestedClass()),
                (new TestValueType<NestedClass>(), new NestedClass()),
                (new TestClass<NestedClassWithParamCtor>(), new NestedClassWithParamCtor(null)),
            };

        private static IEnumerable<bool> IgnoreNullValues
            => new[] { true, false };

        [Theory]
        [MemberData(nameof(TestData), /* enumeratePayloadTweaks: */ false)]
        public static async Task ShouldWorkAtAnyPosition(string json, int bufferSize, Type type, bool ignoreNullValues)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = bufferSize,
                    IgnoreNullValues = ignoreNullValues,
                };

                var array = (ITestObject[])await JsonSerializer.DeserializeAsync(stream, type, readOptions);

                Assert.NotNull(array);
                Assert.Equal(1, array.Length);
                array[0].Verify();
            }
        }

        [Theory]
        [MemberData(nameof(TestData), /* enumeratePayloadTweaks: */ true)]
        public static async Task InvalidNullTokenShouldFailAtAnyPosition(string json, int bufferSize, Type type, bool ignoreNullValues)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            {
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = bufferSize,
                    IgnoreNullValues = ignoreNullValues,
                };

                await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.DeserializeAsync(stream, type, readOptions));
            }
        }

        private static IEnumerable<object[]> TestData(bool enumeratePayloadTweaks)
        {
            // The payload gets padded with leading ' ' chars, so that a continuation
            // happens at every position of the payload. The resulting string is then 
            // passed to the test method.

            // <------min-padding------>[{--payload--}]               min-padding = buffer - payload + 1
            // <-----------2^n byte buffer----------->
            // <-------------max-padding------------>[{--payload--}]  max-padding = buffer - 1

            foreach ((ITestObject TestObject, INestedObject Nested) in TestObjects.Take(enumeratePayloadTweaks ? 1 : TestObjects.Count()))
            {
                Type testObjectType = TestObject.GetType();
                TestObject.Initialize(Nested);

                string payload = JsonSerializer.Serialize(TestObject, testObjectType);

                foreach (Func<string, string> tweak in enumeratePayloadTweaks ? s_payloadTweaks.Skip(1) : s_payloadTweaks.Take(1))
                {
                    payload = tweak(payload);

                    // Wrap the payload inside an array to have something to read before/after.
                    payload = '[' + payload + ']';
                    Type arrayType = Type.GetType(testObjectType.FullName + "[]");

                    // Determine the DefaultBufferSize that is required to contain the complete json.
                    int bufferSize = 16;
                    while (payload.Length > bufferSize)
                    {
                        bufferSize *= 2;
                    }
                    int minPaddingLength = bufferSize - payload.Length + 1;
                    int maxPaddingLength = bufferSize - 1;

                    foreach (int length in Enumerable.Range(minPaddingLength, maxPaddingLength - minPaddingLength + 1))
                    {
                        foreach (bool ignore in IgnoreNullValues)
                        {
                            yield return new object[] { new string(' ', length) + payload, bufferSize, arrayType, ignore };
                        }
                    }
                }
            }
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
            void Initialize(INestedObject nested);
            void Verify();
        }

        private interface INestedObject
        {
            string A { get; set; }
            int B { get; set; }
            void Initialize();
            void Verify();
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

            void ITestObject.Initialize(INestedObject nested)
            {
                A = "Hello";
                B = null;
                C = 42;
                D = null;
                E = 3.14e+17f;
                F = null;
                G = true;
                H = null;
                I = new int[] { 42, 17 };
                (J = (TNested)nested).Initialize();
            }

            void ITestObject.Verify()
            {
                Assert.Equal("Hello", A);
                Assert.Null(B);
                Assert.Equal(42, C);
                Assert.Null(D);
                Assert.Equal(3.14e17f, E);
                Assert.Null(F);
                Assert.True(G);
                Assert.Null(H);
                Assert.Collection(I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(J);
                J.Verify();
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

            void ITestObject.Initialize(INestedObject nested)
            {
                A = "Hello";
                B = null;
                C = 42;
                D = null;
                E = 3.14e+17f;
                F = null;
                G = true;
                H = null;
                I = new int[] { 42, 17 };
                (J = (TNested)nested).Initialize();
            }

            void ITestObject.Verify()
            {
                Assert.Equal("Hello", A);
                Assert.Null(B);
                Assert.Equal(42, C);
                Assert.Null(D);
                Assert.Equal(3.14e17f, E);
                Assert.Null(F);
                Assert.True(G);
                Assert.Null(H);
                Assert.Collection(I, v => Assert.Equal(42, v), v => Assert.Equal(17, v));
                Assert.NotNull(J);
                J.Verify();
            }
        }

        private class NestedClass : INestedObject
        {
            public string A { get; set; }
            public int B { get; set; }

            void INestedObject.Initialize()
            {
                A = null;
                B = 7;
            }

            void INestedObject.Verify()
            {
                Assert.Null(A);
                Assert.Equal(7, B);
            }
        }

        private class NestedClassWithParamCtor : NestedClass
        {
            public NestedClassWithParamCtor(string a)
                => A = a;
        }
    }
}
