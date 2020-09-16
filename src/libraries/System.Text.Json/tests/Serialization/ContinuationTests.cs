// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ContinuationTests
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
                
                await JsonSerializer.SerializeAsync(stream, outer, outerType, new JsonSerializerOptions { Converters = { new OuterConverter<TestClass<NestedClass>>() } });
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
            ITestObject C { get; set; }
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
            ITestObject IOuter.C
            {
                get => C;
                set => C = (TTest)value;
            }

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
            ITestObject IOuter.C
            {
                get => C;
                set => C = (TestClassWithParamCtor)value;
            }

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

        // custom converter to ensure that the padding is written in front of the tested object.
        private class OuterConverter<T> : JsonConverter<IOuter>
        {
            public override IOuter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, IOuter value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("S", value.S);
                writer.WritePropertyName("C");
                JsonSerializer.Serialize<T>(writer, (T)value.C, options);
                writer.WriteEndObject();
            }
        }

        // From https://github.com/dotnet/runtime/issues/42070
        [Theory]
        [InlineData("CustomerSearchApi108KB")]
        [InlineData("CustomerSearchApi107KB")]
        public static async Task ContinuationAtNullToken(string resourceName)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(SR.GetResourceString(resourceName))))
            {
                CustomerCollectionResponse response = await JsonSerializer.DeserializeAsync<CustomerCollectionResponse>(stream, new JsonSerializerOptions { IgnoreNullValues = true });
                Assert.Equal(50, response.Customers.Count);
            }
        }

        private class CustomerCollectionResponse
        {
            [JsonPropertyName("customers")]
            public List<Customer> Customers { get; set; }
        }

        private class CustomerAddress
        {
            [JsonPropertyName("first_name")]
            public string FirstName { get; set; }

            [JsonPropertyName("address1")]
            public string Address1 { get; set; }

            [JsonPropertyName("phone")]
            public string Phone { get; set; }

            [JsonPropertyName("city")]
            public string City { get; set; }

            [JsonPropertyName("zip")]
            public string Zip { get; set; }

            [JsonPropertyName("province")]
            public string Province { get; set; }

            [JsonPropertyName("country")]
            public string Country { get; set; }

            [JsonPropertyName("last_name")]
            public string LastName { get; set; }

            [JsonPropertyName("address2")]
            public string Address2 { get; set; }

            [JsonPropertyName("company")]
            public string Company { get; set; }

            [JsonPropertyName("latitude")]
            public float? Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public float? Longitude { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("country_code")]
            public string CountryCode { get; set; }

            [JsonPropertyName("province_code")]
            public string ProvinceCode { get; set; }
        }

        private class Customer
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("accepts_marketing")]
            public bool AcceptsMarketing { get; set; }

            [JsonPropertyName("created_at")]
            public DateTimeOffset? CreatedAt { get; set; }

            [JsonPropertyName("updated_at")]
            public DateTimeOffset? UpdatedAt { get; set; }

            [JsonPropertyName("first_name")]
            public string FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string LastName { get; set; }

            [JsonPropertyName("orders_count")]
            public int OrdersCount { get; set; }

            [JsonPropertyName("state")]
            public string State { get; set; }

            [JsonPropertyName("total_spent")]
            public string TotalSpent { get; set; }

            [JsonPropertyName("last_order_id")]
            public long? LastOrderId { get; set; }

            [JsonPropertyName("note")]
            public string Note { get; set; }

            [JsonPropertyName("verified_email")]
            public bool VerifiedEmail { get; set; }

            [JsonPropertyName("multipass_identifier")]
            public string MultipassIdentifier { get; set; }

            [JsonPropertyName("tax_exempt")]
            public bool TaxExempt { get; set; }

            [JsonPropertyName("tags")]
            public string Tags { get; set; }

            [JsonPropertyName("last_order_name")]
            public string LastOrderName { get; set; }

            [JsonPropertyName("default_address")]
            public CustomerAddress DefaultAddress { get; set; }

            [JsonPropertyName("addresses")]
            public IList<CustomerAddress> Addresses { get; set; }
        }
    }
}
