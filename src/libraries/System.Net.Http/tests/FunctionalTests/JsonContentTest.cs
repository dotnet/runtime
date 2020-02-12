// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class JsonContentTest
    {
        [Fact]
        public void Ctor_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonContent(null));
        }

        [Fact]
        public async Task Ctor_JsonContent_Accept()
        {
            var content = new JsonContent(new {Success = true});

            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            Stream result = await content.ReadAsStreamAsync();
            Assert.Equal(16, result.Length);
        }

        [Fact]
        public async Task Ctor_UseCustomMediaType_ContentTypeHeaderUpdated()
        {
            var content = new JsonContent(new {Success = true}, "application/custom");

            Assert.Equal("application/custom", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            var destination = new MemoryStream(12);
            await content.CopyToAsync(destination);

            string destinationString = Encoding.UTF8.GetString(destination.ToArray(), 0, (int)destination.Length);

            Assert.Equal(@"{""Success"":true}", destinationString);
        }

        [Fact]
        public async Task Ctor_UseSerializerOptions_ContentSerializedCorrectly()
        {
            var content = new JsonContent(new { Success = true }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var destination = new MemoryStream(12);
            await content.CopyToAsync(destination);

            string destinationString = Encoding.UTF8.GetString(destination.ToArray(), 0, (int)destination.Length);

            Assert.Equal(@"{""success"":true}", destinationString);
        }
    }
}
