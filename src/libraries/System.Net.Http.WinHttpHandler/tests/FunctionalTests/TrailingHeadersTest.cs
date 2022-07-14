// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Functional.Tests;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public class TrailingHeadersTest : HttpClientHandlerTestBase
    {
        public TrailingHeadersTest(ITestOutputHelper output) : base(output)
        { }

        // Build number suggested by the WinHttp team.
        // It can be reduced after the backport of WINHTTP_QUERY_FLAG_TRAILERS is finished,
        // and the patches are rolled out to CI machines.
        public static bool OsSupportsWinHttpTrailingHeaders => Environment.OSVersion.Version >= new Version(10, 0, 19622, 0);

        public static bool TestsEnabled => OsSupportsWinHttpTrailingHeaders && PlatformDetection.SupportsAlpn;

        protected override Version UseVersion => new Version(2, 0);

        protected static byte[] DataBytes = "data"u8.ToArray();

        protected static readonly IList<HttpHeaderData> TrailingHeaders = new HttpHeaderData[] {
            new HttpHeaderData("MyCoolTrailerHeader", "amazingtrailer"),
            new HttpHeaderData("EmptyHeader", ""),
            new HttpHeaderData("Accept-Encoding", "identity,gzip"),
            new HttpHeaderData("Hello", "World") };

        protected static Frame MakeDataFrame(int streamId, byte[] data, bool endStream = false) =>
            new DataFrame(data, (endStream ? FrameFlags.EndStream : FrameFlags.None), 0, streamId);

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task Http2GetAsync_NoTrailingHeaders_EmptyCollection()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: true));

                // Server doesn't send trailing header frame.
                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var trailingHeaders = GetTrailingHeaders(response);
                Assert.NotNull(trailingHeaders);
                Assert.Equal(0, trailingHeaders.Count());
            }
        }

        [InlineData(false)]
        [InlineData(true)]
        [ConditionalTheory(nameof(TestsEnabled))]
        public async Task Http2GetAsync_MissingTrailer_TrailingHeadersAccepted(bool responseHasContentLength)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                if (responseHasContentLength)
                {
                    await connection.SendResponseHeadersAsync(streamId, endStream: false, headers: new[] { new HttpHeaderData("Content-Length", DataBytes.Length.ToString()) });
                }
                else
                {
                    await connection.SendDefaultResponseHeadersAsync(streamId);
                }

                // Response data, missing Trailers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));

                // Additional trailing header frame.
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader: true, headers: TrailingHeaders, endStream: true);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var trailingHeaders = GetTrailingHeaders(response);
                Assert.Equal(TrailingHeaders.Count, trailingHeaders.Count());
                Assert.Contains("amazingtrailer", trailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", trailingHeaders.GetValues("Hello"));
            }
        }

        [InlineData(false)]
        [InlineData(true)]
        [ConditionalTheory(nameof(TestsEnabled))]
        public async Task Http2GetAsyncResponseHeadersReadOption_TrailingHeaders_Available(bool responseHasContentLength)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                if (responseHasContentLength)
                {
                    await connection.SendResponseHeadersAsync(streamId, endStream: false, headers: new[] { new HttpHeaderData("Content-Length", DataBytes.Length.ToString()) });
                }
                else
                {
                    await connection.SendDefaultResponseHeadersAsync(streamId);
                }

                // Response data, missing Trailers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Pending read on the response content.
                var trailingHeaders = GetTrailingHeaders(response);
                Assert.True(trailingHeaders == null || trailingHeaders.Count() == 0);

                Stream stream = await response.Content.ReadAsStreamAsync(TestAsync);
                Byte[] data = new Byte[100];
                await stream.ReadAsync(data, 0, data.Length);

                // Intermediate test - haven't reached stream EOF yet.
                trailingHeaders = GetTrailingHeaders(response);
                Assert.True(trailingHeaders == null || trailingHeaders.Count() == 0);

                // Finish data stream and write out trailing headers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));
                await connection.SendResponseHeadersAsync(streamId, endStream: true, isTrailingHeader: true, headers: TrailingHeaders);

                // Read data until EOF is reached
                while (stream.Read(data, 0, data.Length) != 0) ;

                trailingHeaders = GetTrailingHeaders(response);
                Assert.Equal(TrailingHeaders.Count, trailingHeaders.Count());
                Assert.Contains("amazingtrailer", trailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", trailingHeaders.GetValues("Hello"));

                // Read when already zero. Trailers shouldn't be changed.
                stream.Read(data, 0, data.Length);

                trailingHeaders = GetTrailingHeaders(response);
                Assert.Equal(TrailingHeaders.Count, trailingHeaders.Count());
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingHeaderNoBody()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.SendResponseHeadersAsync(streamId, endStream: true, isTrailingHeader: true, headers: TrailingHeaders);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var trailingHeaders = GetTrailingHeaders(response);
                Assert.Equal(TrailingHeaders.Count, trailingHeaders.Count());
                Assert.Contains("amazingtrailer", trailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", trailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task Http2GetAsync_TrailingHeaders_NoData_EmptyResponseObserved()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // No data.

                // Response trailing headers
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader: true, headers: TrailingHeaders);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal<byte>(Array.Empty<byte>(), await response.Content.ReadAsByteArrayAsync());

                var trailingHeaders = GetTrailingHeaders(response);
                Assert.Contains("amazingtrailer", trailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", trailingHeaders.GetValues("Hello"));
            }
        }

        private HttpHeaders GetTrailingHeaders(HttpResponseMessage responseMessage)
        {
#if !NET48
            return responseMessage.TrailingHeaders;
#else
#pragma warning disable CS0618 // Type or member is obsolete
            responseMessage.RequestMessage.Properties.TryGetValue("__ResponseTrailers", out object trailers);
#pragma warning restore CS0618 // Type or member is obsolete
            return (HttpHeaders)trailers;
#endif
        }
    }
}
