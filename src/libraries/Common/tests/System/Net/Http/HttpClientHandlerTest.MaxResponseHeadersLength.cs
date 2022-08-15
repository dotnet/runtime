// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
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
        public HttpClientHandler_MaxResponseHeadersLength_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(1024)]
        [InlineData(10240)]
        public async Task Http3Test(int? maxResponseHeadersLength)
        {
            if (UseVersion.Major != 3) return;

            var requestCts = new CancellationTokenSource();
            var controlStreamEstablishedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);

                if (maxResponseHeadersLength.HasValue)
                {
                    handler.MaxResponseHeadersLength = maxResponseHeadersLength.Value;
                }

                await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync(uri, requestCts.Token));

                await controlStreamEstablishedTcs.Task.WaitAsync(TestHelper.PassingTestTimeout);
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    requestCts.Cancel();

                    var http3Connection = (Http3LoopbackConnection)connection;

                    await http3Connection.EnsureControlStreamAcceptedAsync().WaitAsync(TestHelper.PassingTestTimeout);

                    controlStreamEstablishedTcs.SetResult();
                });
            });
        }

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

                Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri)).WaitAsync(TestHelper.PassingTestTimeout);
                if (!IsWinHttpHandler)
                {
                    Assert.Contains((handler.MaxResponseHeadersLength * 1024).ToString(), e.ToString());
                }
            },
            async server =>
            {
                HttpHeaderData[] headers = new[] { new HttpHeaderData("Foo", new string('a', handler.MaxResponseHeadersLength * 1024)) };

                try
                {
                    await server.HandleRequestAsync(headers: headers).WaitAsync(TestHelper.PassingTestTimeout);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                }
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
                    await client.GetAsync(uri).WaitAsync(TestHelper.PassingTestTimeout);
                }
                else
                {
                    Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri)).WaitAsync(TestHelper.PassingTestTimeout);
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
                    await server.HandleRequestAsync(headers: headers).WaitAsync(TestHelper.PassingTestTimeout);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                }
            });
        }
    }
}
