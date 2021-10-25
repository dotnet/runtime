// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class PropertyNameTestsDynamic : PropertyNameTests
    {
        public PropertyNameTestsDynamic() : base(JsonSerializerWrapperForString.StringSerializer) { }

        [Fact]
        public async Task JsonNullNameAttribute()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;

            // A null name in JsonPropertyNameAttribute is not allowed.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(new NullPropertyName_TestClass(), options));
        }

        [Fact]
        public async Task JsonNameConflictOnCaseInsensitiveFail()
        {
            string json = @"{""myInt"":1,""MyInt"":2}";

            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<IntPropertyNamesDifferentByCaseOnly_TestClass>(json, options));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(new IntPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }
        }
    }
}
