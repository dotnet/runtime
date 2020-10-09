// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json.Test
{
    public class EmptyObjectTest
    {
        [Fact]
        public void EmptyObject_AddsEmptyString()
        {
            var json = @"{
                ""key"": { },
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.True(jsonConfigSource.TryGet("key", out _));
        }

        [Fact]
        public void NullObject_AddsAsNull()
        {
            var json = @"{
                ""key"": null,
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Null(jsonConfigSource.Get("key"));
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

        [Fact]
        public void EmptyArray_AddsEmptyString()
        {
            var json = @"{
                ""ip"": [ ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("", jsonConfigSource.Get("ip"));
        }


        [Fact]
        public void NotEmptyArray_DoesNotAddParent()
        {
            var json = @"{
                ""ip"": [
                    ""1.2.3.4""
                ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.False(jsonConfigSource.TryGet("ip", out _));
            Assert.True(jsonConfigSource.TryGet("ip:0", out _));
            Assert.Equal("1.2.3.4", jsonConfigSource.Get("ip:0"));
        }
    }
}
