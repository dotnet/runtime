// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MyNamespace;
using System.Text.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests.CodeGen
{
    public class LocationPocoTests
    {
        [Fact]
        public static void RoundTrip()
        {
            Location expected = Create();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.Location);
            Location obj = JsonSerializer.Deserialize(json, JsonContext.Default.Location);

            Verify(expected, obj);
        }

        internal static void Verify(Location expected, Location obj)
        {
            Assert.Equal(expected.Address1, obj.Address1);
            Assert.Equal(expected.Address2, obj.Address2);
            Assert.Equal(expected.City, obj.City);
            Assert.Equal(expected.State, obj.State);
            Assert.Equal(expected.PostalCode, obj.PostalCode);
            Assert.Equal(expected.Name, obj.Name);
            Assert.Equal(expected.PhoneNumber, obj.PhoneNumber);
            Assert.Equal(expected.Country, obj.Country);
        }

        private static Location Create()
        {
            return new Location
            {
                Id = 1234,
                Address1 = "The Street Name",
                Address2 = "20/11",
                City = "The City",
                State = "The State",
                PostalCode = "abc-12",
                Name = "Nonexisting",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };
        }
    }
}
