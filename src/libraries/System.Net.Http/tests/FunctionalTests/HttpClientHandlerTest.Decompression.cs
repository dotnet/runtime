// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.Compression;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Decompression_Test : HttpClientTestBase
    {
        [Fact]
        public async Task Brotli_DecompressesResponse_Success()
        {
            var expectedContent = new byte[12345];
            new Random(42).NextBytes(expectedContent);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                using (HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                using (var decodedStream = new BrotliStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
                {
                    var data = new MemoryStream();
                    await decodedStream.CopyToAsync(data);
                    Assert.Equal(expectedContent, data.ToArray());
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.Writer.WriteAsync("HTTP/1.1 200 OK\r\nContent-Encoding: br\r\n\r\n");
                    using (var brotli = new BrotliStream(connection.Stream, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        await brotli.WriteAsync(expectedContent);
                    }
                });
            });
        }

        [OuterLoop("Accessing remote server")]
        [Fact]
        public async Task Brotli_External_DecompressesResponse_Success()
        {
            const string BrotliUrl = "http://httpbin.org/brotli";

            var message = new HttpRequestMessage(HttpMethod.Get, BrotliUrl);
            message.Headers.TryAddWithoutValidation("SomeAwesomeHeader", "AndItsAwesomeValue");

            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.SendAsync(message))
            {
                Assert.Contains("br", response.Content.Headers.ContentEncoding);
                using (var decodedStream = new BrotliStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
                using (var reader = new StreamReader(decodedStream))
                {
                    string respText = await reader.ReadToEndAsync();
                    Assert.Contains("AndItsAwesomeValue", respText);
                }
            }
        }
    }
}
