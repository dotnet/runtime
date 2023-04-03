// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
#if !WINHTTPHANDLER_TEST
using System.Net.Quic;
#endif
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandlerTestBase
    {
        private const int Http3ExcessiveLoad = 0x107;

        public HttpClientHandler_MaxResponseHeadersLength_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InvalidValue_ThrowsException(int invalidValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = invalidValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(65)]
        [InlineData(int.MaxValue)]
        public void ValidValue_SetGet_Roundtrips(int validValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.MaxResponseHeadersLength = validValue;
                Assert.Equal(validValue, handler.MaxResponseHeadersLength);
            }
        }

        [Fact]
        public async Task SetAfterUse_Throws()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);

                handler.MaxResponseHeadersLength = 1;
                (await client.GetStreamAsync(uri)).Dispose();
                Assert.Throws<InvalidOperationException>(() => handler.MaxResponseHeadersLength = 1);
            },
            server => server.AcceptConnectionSendResponseAndCloseAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        public async Task LargeSingleHeader_ThrowsException(int maxResponseHeadersLength)
        {
            using HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxResponseHeadersLength = maxResponseHeadersLength;

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient(handler);

                Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                if (!IsWinHttpHandler)
                {
                    Assert.Contains((handler.MaxResponseHeadersLength * 1024).ToString(), e.ToString());
                }
            },
            async server =>
            {
                try
                {
                    await server.HandleRequestAsync(headers: new[] { new HttpHeaderData("Foo", new string('a', handler.MaxResponseHeadersLength * 1024)) });
                }
                // Client can respond by closing/aborting the underlying stream while we are still sending the headers, ignore these exceptions
                catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.Shutdown) { }
#if !WINHTTPHANDLER_TEST
                catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == Http3ExcessiveLoad) {}
#endif
            });
        }

        [Theory]
        [InlineData(null, 63 * 1024)]
        [InlineData(null, 65 * 1024)]
        [InlineData(1, 100)]
        [InlineData(1, 1024)]
        [InlineData(int.MaxValue / 800, 100 * 1024)] // Capped at int.MaxValue
        public async Task ThresholdExceeded_ThrowsException(int? maxResponseHeadersLength, int headersLengthEstimate)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();

                if (maxResponseHeadersLength.HasValue)
                {
                    handler.MaxResponseHeadersLength = maxResponseHeadersLength.Value;
                }

                using HttpClient client = CreateHttpClient(handler);

                if (headersLengthEstimate < handler.MaxResponseHeadersLength * 1024L)
                {
                    await client.GetAsync(uri);
                }
                else
                {
                    Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                    if (!IsWinHttpHandler)
                    {
                        Assert.Contains((handler.MaxResponseHeadersLength * 1024).ToString(), e.ToString());
                    }
                }
            },
            async server =>
            {
                var headers = new List<HttpHeaderData>();
                for (int i = 0; i <= headersLengthEstimate / 500; i++)
                {
                    headers.Add(new HttpHeaderData($"Custom-{i}", new string('a', 480)));
                }

                try
                {
                    await server.HandleRequestAsync(headers: headers);
                }
                // Client can respond by closing/aborting the underlying stream while we are still sending the headers, ignore these exceptions
                catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.Shutdown) { }
#if !WINHTTPHANDLER_TEST
                catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == Http3ExcessiveLoad) {}
#endif
            });
        }
    }
}
