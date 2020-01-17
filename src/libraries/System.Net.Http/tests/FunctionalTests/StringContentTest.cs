// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class StringContentTest
    {
        [Fact]
        public void Ctor_NullString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StringContent(null));
        }

        [Fact]
        public async Task Ctor_EmptyString_Accept()
        {
            // Consider empty strings like null strings (null and empty strings should be treated equally).
            var content = new StringContent(string.Empty);
            Stream result = await content.ReadAsStreamAsync();
            Assert.Equal(0, result.Length);
        }

        [Fact]
        public async Task Ctor_UseCustomEncodingAndMediaType_EncodingUsedAndContentTypeHeaderUpdated()
        {
            // Use UTF-8 encoding to serialize a chinese string.
            string sourceString = "\u4f1a\u5458\u670d\u52a1";

            var content = new StringContent(sourceString, Encoding.UTF8, "application/custom");

            Assert.Equal("application/custom", content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", content.Headers.ContentType.CharSet);

            var destination = new MemoryStream(12);
            await content.CopyToAsync(destination);

            string destinationString = Encoding.UTF8.GetString(destination.ToArray(), 0, (int)destination.Length);

            Assert.Equal(sourceString, destinationString);
        }

        [Fact]
        public async Task Ctor_DefineNoEncoding_DefaultEncodingUsed()
        {
            string sourceString = "\u00C4\u00E4\u00FC\u00DC";
            var content = new StringContent(sourceString);
            Encoding defaultStringEncoding = Encoding.GetEncoding("utf-8");

            // If no encoding is defined, the default encoding is used: utf-8
            Assert.Equal("text/plain", content.Headers.ContentType.MediaType);
            Assert.Equal(defaultStringEncoding.WebName, content.Headers.ContentType.CharSet);

            // Make sure the default encoding is also used when serializing the content.
            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            Assert.Equal(8, destination.Length);

            destination.Seek(0, SeekOrigin.Begin);
            string roundTrip = new StreamReader(destination, defaultStringEncoding).ReadToEnd();
            Assert.Equal(sourceString, roundTrip);
        }

        [Fact]
        public void Ctor_UseMediaTypeObjectWithCustomMediaTypeAndUseCustomEncoding_ContentHeaderIsRenderedWithCustomMediaTypeAndCustomCharset()
        {
            var mediaType = new MediaTypeHeaderValue("application/custom");
            var content = new StringContent("source", Encoding.UTF7, mediaType);

            // Make sure the charset label is present with custom encoding when the Content-type header is rendered
            Assert.Equal("application/custom; charset=utf-7", content.Headers.ContentType.ToString());
        }

        [Fact]
        public void Ctor_UseMediaTypeObjectWithEmptyCharsetAndNoEncoding_ContentHeaderIsRenderedWithoutCharset()
        {
            var mediaType = new MediaTypeHeaderValue("text/plain");
            mediaType.CharSet = string.Empty;
            var content = new StringContent("source", mediaType);

            // Make sure the charset label is omitted when the Content-type header is rendered
            Assert.Equal("text/plain", content.Headers.ContentType.ToString());
        }
    }
}
