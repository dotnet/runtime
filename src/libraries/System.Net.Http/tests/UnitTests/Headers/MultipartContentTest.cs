// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class MultipartContentTest
    {
        public static IEnumerable<object[]> MultipartContent_TestData()
        {
            var multipartContents = new List<MultipartContent>();

            var complexContent = new MultipartContent();

            var stringContent = new StringContent("bar1");
            stringContent.Headers.Add("latin1", "\U0001F600");
            complexContent.Add(stringContent);

            var byteArrayContent = new ByteArrayContent("bar2"u8.ToArray());
            byteArrayContent.Headers.Add("utf8", "\U0001F600");
            complexContent.Add(byteArrayContent);

            byteArrayContent = new ByteArrayContent("bar3"u8.ToArray());
            byteArrayContent.Headers.Add("ascii", "\U0001F600");
            complexContent.Add(byteArrayContent);

            byteArrayContent = new ByteArrayContent("bar4"u8.ToArray());
            byteArrayContent.Headers.Add("default", "\U0001F600");
            complexContent.Add(byteArrayContent);

            stringContent = new StringContent("bar5");
            stringContent.Headers.Add("foo", "bar");
            complexContent.Add(stringContent);

            multipartContents.Add(complexContent);
            multipartContents.Add(new MultipartContent());
            multipartContents.Add(new MultipartFormDataContent());

            var encodingSelectors = new HeaderEncodingSelector<HttpContent>[]
            {
                (_, _) => null,
                (_, _) => Encoding.ASCII,
                (_, _) => Encoding.Latin1,
                (_, _) => Encoding.UTF8,
                (name, _) => name switch
                {
                    "latin1" => Encoding.Latin1,
                    "utf8" => Encoding.UTF8,
                    "ascii" => Encoding.ASCII,
                    _ => null
                }
            };

            foreach (MultipartContent multipartContent in multipartContents)
            {
                foreach (HeaderEncodingSelector<HttpContent> encodingSelector in encodingSelectors)
                {
                    multipartContent.HeaderEncodingSelector = encodingSelector;
                    yield return new object[] { multipartContent };
                }
            }
        }

        [Theory]
        [MemberData(nameof(MultipartContent_TestData))]
        public async Task MultipartContent_TryComputeLength_ReturnsSameLengthAsCopyToAsync(MultipartContent multipartContent)
        {
            Assert.True(multipartContent.TryComputeLength(out long length));

            var copyToStream = new MemoryStream();
            multipartContent.CopyTo(copyToStream, context: null, cancellationToken: default);
            Assert.Equal(length, copyToStream.Length);

            var copyToAsyncStream = new MemoryStream();
            await multipartContent.CopyToAsync(copyToAsyncStream, context: null, cancellationToken: default);
            Assert.Equal(length, copyToAsyncStream.Length);

            Assert.Equal(copyToStream.ToArray(), copyToAsyncStream.ToArray());
        }
    }
}
