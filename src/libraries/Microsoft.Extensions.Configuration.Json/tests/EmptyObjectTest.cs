// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json.Test
{
    public class EmptyObjectTest
    {
        [Fact]
        public void EmptyObject_AddsAsNull()
        {
            var json = @"{
                ""key"": { },
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Null(jsonConfigSource.Get("key"));
        }

        [Fact]
        public void NullObject_AddsEmptyString()
        {
            var json = @"{
                ""key"": null,
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("", jsonConfigSource.Get("key"));
        }

        [Fact]
        public void NestedObject_DoesNotAddParent()
        {
            var json = @"{
                ""key"": {
                    ""nested"": ""value""
                },
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.False(jsonConfigSource.TryGet("key", out _));
            Assert.Equal("value", jsonConfigSource.Get("key:nested"));
        }
    }
}
