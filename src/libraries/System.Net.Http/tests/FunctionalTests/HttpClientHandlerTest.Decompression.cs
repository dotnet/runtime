// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Decompression_Test : HttpClientTestBase
    {
        private readonly ITestOutputHelper _output;

        public HttpClientHandler_Decompression_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> DecompressedResponse_MethodSpecified_DecompressedContentReturned_MemberData()
        {
            foreach (bool specifyAllMethods in new[] { false, true })
            {
                yield return new object[]
                {
                    "deflate",
                    new Func<Stream, Stream>(s => new DeflateStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    specifyAllMethods ? DecompressionMethods.Deflate : DecompressionMethods.All
                };
                yield return new object[]
                {
                    "gzip",
                    new Func<Stream, Stream>(s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    specifyAllMethods ? DecompressionMethods.GZip : DecompressionMethods.All
                };
                yield return new object[]
                {
                    "br",
                    new Func<Stream, Stream>(s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    specifyAllMethods ? DecompressionMethods.Brotli : DecompressionMethods.All
                };
            }
        }

        [Theory]
        [MemberData(nameof(DecompressedResponse_MethodSpecified_DecompressedContentReturned_MemberData))]
        public async Task DecompressedResponse_MethodSpecified_DecompressedContentReturned(
            string encodingName, Func<Stream, Stream> compress, DecompressionMethods methods)
        {
            if (!UseSocketsHttpHandler && encodingName == "br")
            {
                // Brotli only supported on SocketsHttpHandler.
                return;
            }

            var expectedContent = new byte[12345];
            new Random(42).NextBytes(expectedContent);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.AutomaticDecompression = methods;
                    Assert.Equal<byte>(expectedContent, await client.GetByteArrayAsync(uri));
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.Writer.WriteAsync($"HTTP/1.1 200 OK\r\nContent-Encoding: {encodingName}\r\n\r\n");
                    using (Stream compressedStream = compress(connection.Stream))
                    {
                        await compressedStream.WriteAsync(expectedContent);
                    }
                });
            });
        }

        public static IEnumerable<object[]> DecompressedResponse_MethodNotSpecified_OriginalContentReturned_MemberData()
        {
            yield return new object[]
            {
                "deflate",
                new Func<Stream, Stream>(s => new DeflateStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                DecompressionMethods.None
            };
            yield return new object[]
            {
                "gzip",
                new Func<Stream, Stream>(s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                DecompressionMethods.Brotli
            };
            yield return new object[]
            {
                "br",
                new Func<Stream, Stream>(s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                DecompressionMethods.Deflate | DecompressionMethods.GZip
            };
        }

        [Theory]
        [MemberData(nameof(DecompressedResponse_MethodNotSpecified_OriginalContentReturned_MemberData))]
        public async Task DecompressedResponse_MethodNotSpecified_OriginalContentReturned(
            string encodingName, Func<Stream, Stream> compress, DecompressionMethods methods)
        {
            if (IsCurlHandler && encodingName == "br")
            {
                // 'Content-Encoding' response header with Brotli causes error
                // with some Linux distros of libcurl.
                return;
            }

            var expectedContent = new byte[12345];
            new Random(42).NextBytes(expectedContent);

            var compressedContentStream = new MemoryStream();
            using (Stream s = compress(compressedContentStream))
            {
                await s.WriteAsync(expectedContent);
            }
            byte[] compressedContent = compressedContentStream.ToArray();

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.AutomaticDecompression = methods;
                    Assert.Equal<byte>(compressedContent, await client.GetByteArrayAsync(uri));
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.Writer.WriteAsync($"HTTP/1.1 200 OK\r\nContent-Encoding: {encodingName}\r\n\r\n");
                    await connection.Stream.WriteAsync(compressedContent);
                });
            });
        }
    }
}
