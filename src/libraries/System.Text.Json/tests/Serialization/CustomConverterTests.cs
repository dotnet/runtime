// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void MultipleConvertersInObjectArray()
        {
            const string expectedJson = @"[""?"",{""TypeDiscriminator"":1,""CreditLimit"":100,""Name"":""C""},null]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyBoolEnumConverter());
            options.Converters.Add(new PersonConverter());

            Customer customer = new Customer();
            customer.CreditLimit = 100;
            customer.Name = "C";

            MyBoolEnum myBoolEnum = MyBoolEnum.Unknown;
            MyBoolEnum? myNullBoolEnum = null;

            string json = JsonSerializer.ToString(new object[] { myBoolEnum, customer, myNullBoolEnum }, options);
            Assert.Equal(expectedJson, json);

            JsonElement jsonElement = JsonSerializer.Parse<JsonElement>(json, options);
            string jsonElementString = jsonElement.ToString();
            Assert.Equal(expectedJson, jsonElementString);
        }
    }
}
