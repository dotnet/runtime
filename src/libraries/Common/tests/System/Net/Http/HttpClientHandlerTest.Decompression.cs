// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Test.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_Decompression_Test : HttpClientHandlerTestBase
    {
#if !NETFRAMEWORK
        private static readonly DecompressionMethods _all = DecompressionMethods.All;
#else
        private static readonly DecompressionMethods _all = DecompressionMethods.Deflate | DecompressionMethods.GZip;
#endif
        public HttpClientHandler_Decompression_Test(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> DecompressedResponse_MethodSpecified_DecompressedContentReturned_MemberData() =>
            from compressionName in new[] { "gzip", "GZIP", "zlib", "ZLIB", "deflate", "DEFLATE", "br", "BR" }
            from all in new[] { false, true }
            from copyTo in new[] { false, true }
            from contentLength in new[] { 0, 1, 12345 }
            select new object[] { compressionName, all, copyTo, contentLength };

        [Theory]
        [MemberData(nameof(DecompressedResponse_MethodSpecified_DecompressedContentReturned_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task DecompressedResponse_MethodSpecified_DecompressedContentReturned(string compressionName, bool all, bool useCopyTo, int contentLength)
        {
            if (IsWinHttpHandler &&
                (compressionName is "br" or "BR" or "zlib" or "ZLIB"))
            {
                // brotli and zlib not supported on WinHttpHandler
                return;
            }

            Func<Stream, Stream> compress;
            DecompressionMethods methods;
            string encodingName = compressionName;
            switch (compressionName)
            {
                case "gzip":
                case "GZIP":
                    compress = s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true);
                    methods = all ? DecompressionMethods.GZip : _all;
                    break;

#if !NETFRAMEWORK
                case "br":
                case "BR":
                    compress = s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true);
                    methods = all ? DecompressionMethods.Brotli : _all;
                    break;

                case "zlib":
                case "ZLIB":
                    compress = s => new ZLibStream(s, CompressionLevel.Optimal, leaveOpen: true);
                    methods = all ? DecompressionMethods.Deflate : _all;
                    encodingName = "deflate";
                    break;
#endif

                case "deflate":
                case "DEFLATE":
                    compress = s => new DeflateStream(s, CompressionLevel.Optimal, leaveOpen: true);
                    methods = all ? DecompressionMethods.Deflate : _all;
                    break;

                default:
                    throw new Exception($"Unexpected compression: {compressionName}");
            }

            var expectedContent = new byte[contentLength];
            new Random(42).NextBytes(expectedContent);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.AutomaticDecompression = methods;
                    AssertExtensions.SequenceEqual(expectedContent, await client.GetByteArrayAsync(TestAsync, useCopyTo, uri));
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.WriteStringAsync($"HTTP/1.1 200 OK\r\nContent-Encoding: {encodingName}\r\n\r\n");
                    using (Stream compressedStream = compress(connection.Stream))
                    {
                        await compressedStream.WriteAsync(expectedContent);
                    }
                });
            });
        }

        public static IEnumerable<object[]> DecompressedResponse_MethodNotSpecified_OriginalContentReturned_MemberData()
        {
            foreach (bool useCopyTo in new[] { false, true })
            {
                yield return new object[]
                {
                    "gzip",
                    new Func<Stream, Stream>(s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    DecompressionMethods.None,
                    useCopyTo
                };
#if !NETFRAMEWORK
                yield return new object[]
                {
                    "deflate",
                    new Func<Stream, Stream>(s => new ZLibStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    DecompressionMethods.Brotli,
                    useCopyTo
                };
                yield return new object[]
                {
                    "br",
                    new Func<Stream, Stream>(s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true)),
                    DecompressionMethods.Deflate | DecompressionMethods.GZip,
                    useCopyTo
                };
#endif
            }
        }

        [Theory]
        [MemberData(nameof(DecompressedResponse_MethodNotSpecified_OriginalContentReturned_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task DecompressedResponse_MethodNotSpecified_OriginalContentReturned(
            string encodingName, Func<Stream, Stream> compress, DecompressionMethods methods, bool useCopyTo)
        {
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
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.AutomaticDecompression = methods;
                    AssertExtensions.SequenceEqual(compressedContent, await client.GetByteArrayAsync(TestAsync, useCopyTo, uri));
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.WriteStringAsync($"HTTP/1.1 200 OK\r\nContent-Encoding: {encodingName}\r\n\r\n");
                    await connection.Stream.WriteAsync(compressedContent);
                });
            });
        }

        [Theory]
        [InlineData("gzip", DecompressionMethods.GZip)]
#if !NETFRAMEWORK
        [InlineData("deflate", DecompressionMethods.Deflate)]
        [InlineData("br", DecompressionMethods.Brotli)]
#endif
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task DecompressedResponse_EmptyBody_Success(string encodingName, DecompressionMethods methods)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.AutomaticDecompression = methods;
                    Assert.Equal(Array.Empty<byte>(), await client.GetByteArrayAsync(TestAsync, useCopyTo: false, uri));
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.WriteStringAsync($"HTTP/1.1 200 OK\r\nContent-Encoding: {encodingName}\r\n\r\n");
                });
            });
        }

        [Theory]
#if NETCOREAPP
        [InlineData(DecompressionMethods.Brotli, "br", "")]
        [InlineData(DecompressionMethods.Brotli, "br", "br")]
        [InlineData(DecompressionMethods.Brotli, "br", "gzip")]
        [InlineData(DecompressionMethods.Brotli, "br", "gzip, deflate")]
#endif
        [InlineData(DecompressionMethods.GZip, "gzip", "")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "")]
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip, deflate", "")]
        [InlineData(DecompressionMethods.GZip, "gzip", "gzip")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "deflate")]
        [InlineData(DecompressionMethods.GZip, "gzip", "deflate")]
        [InlineData(DecompressionMethods.GZip, "gzip", "br")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "gzip")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "br")]
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip, deflate", "gzip, deflate")]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task GetAsync_SetAutomaticDecompression_AcceptEncodingHeaderSentWithNoDuplicates(
            DecompressionMethods methods,
            string encodings,
            string manualAcceptEncodingHeaderValues)
        {
            // Brotli only supported on SocketsHttpHandler.
            if (IsWinHttpHandler && (encodings.Contains("br") || manualAcceptEncodingHeaderValues.Contains("br")))
            {
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.AutomaticDecompression = methods;

                using (HttpClient client = CreateHttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(manualAcceptEncodingHeaderValues))
                    {
                        client.DefaultRequestHeaders.Add("Accept-Encoding", manualAcceptEncodingHeaderValues);
                    }

                    Task<HttpResponseMessage> clientTask = client.SendAsync(TestAsync, CreateRequest(HttpMethod.Get, url, UseVersion));
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new Task[] { clientTask, serverTask });

                    List<string> requestLines = await serverTask;
                    string requestLinesString = string.Join("\r\n", requestLines);
                    _output.WriteLine(requestLinesString);

                    Assert.InRange(Regex.Matches(requestLinesString, "Accept-Encoding").Count, 1, 1);
                    Assert.InRange(Regex.Matches(requestLinesString, encodings).Count, 1, 1);
                    if (!string.IsNullOrEmpty(manualAcceptEncodingHeaderValues))
                    {
                        Assert.InRange(Regex.Matches(requestLinesString, manualAcceptEncodingHeaderValues).Count, 1, 1);
                    }

                    using (HttpResponseMessage response = await clientTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            });
        }

        [Theory]
#if NETCOREAPP
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli, "gzip; q=1.0, deflate; q=1.0, br; q=1.0", "")]
#endif
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip; q=1.0, deflate; q=1.0", "")]
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip; q=1.0", "deflate")]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task GetAsync_SetAutomaticDecompression_AcceptEncodingHeaderSentWithQualityWeightingsNoDuplicates(
            DecompressionMethods methods,
            string manualAcceptEncodingHeaderValues,
            string expectedHandlerAddedAcceptEncodingHeaderValues)
        {
            if (IsWinHttpHandler)
            {
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.AutomaticDecompression = methods;

                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept-Encoding", manualAcceptEncodingHeaderValues);

                    Task<HttpResponseMessage> clientTask = client.SendAsync(TestAsync, CreateRequest(HttpMethod.Get, url, UseVersion));
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new Task[] { clientTask, serverTask });

                    List<string> requestLines = await serverTask;
                    string requestLinesString = string.Join("\r\n", requestLines);
                    _output.WriteLine(requestLinesString);

                    bool acceptEncodingValid = false;
                    foreach (string requestLine in requestLines)
                    {
                        if (requestLine.StartsWith("Accept-Encoding", StringComparison.OrdinalIgnoreCase))
                        {
                            acceptEncodingValid = requestLine.Equals($"Accept-Encoding: {manualAcceptEncodingHeaderValues}{(string.IsNullOrEmpty(expectedHandlerAddedAcceptEncodingHeaderValues) ? string.Empty : ", " + expectedHandlerAddedAcceptEncodingHeaderValues)}", StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                    
                    Assert.True(acceptEncodingValid, "Accept-Encoding missing or invalid");

                    using (HttpResponseMessage response = await clientTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            });
        }
    }
}
