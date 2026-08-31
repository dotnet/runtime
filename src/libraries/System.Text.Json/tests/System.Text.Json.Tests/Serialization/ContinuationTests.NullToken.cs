// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ContinuationTests
    {
        // From https://github.com/dotnet/runtime/issues/42070
        [Theory]
        [MemberData(nameof(ContinuationAtNullTokenTestData))]
        public static async Task ContinuationAtNullToken(string payload)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
            {
                CustomerCollectionResponse response = await JsonSerializer.DeserializeAsync<CustomerCollectionResponse>(stream, new JsonSerializerOptions { IgnoreNullValues = true });
                Assert.Equal(50, response.Customers.Count);
            }
        }

        public static IEnumerable<object[]> ContinuationAtNullTokenTestData
            => new[]
            {
                new[] { SR.CustomerSearchApi108KB },
                new[] { SR.CustomerSearchApi107KB },
            };

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
