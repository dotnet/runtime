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
            for (int repeat = 0; repeat <= (UseVersion.Major == 3 ? 50 : 1); repeat++)
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
        }

        public static IEnumerable<object[]> LargeSingleHeader_ThrowsException_MemberData()
        {
            object[][] options = new[]
            {
                new object[] { 1 },
                new object[] { 2 },
                new object[] { 15 },
            };

            var rng = new Random();
            int count = options.Length * 50;

            for (int i = 0; i < count; i++)
            {
                yield return options[rng.Next(options.Length)];
            }
        }

        [Theory]
        [MemberData(nameof(LargeSingleHeader_ThrowsException_MemberData))]
        public async Task LargeSingleHeader_ThrowsException(int maxResponseHeadersLength)
        {
            if (UseVersion.Major != 3) return;

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

        public static IEnumerable<object[]> ThresholdExceeded_ThrowsException_MemberData()
        {
            object[][] options = new[]
            {
                new object[] { null, 63 * 1024 },
                new object[] { null, 65 * 1024 },
                new object[] { 1, 100 },
                new object[] { 1, 1024 },
                new object[] { 2, 1100 },
                new object[] { 2, 2048 },
                new object[] { int.MaxValue / 800, 100 * 1024 },
            };

            var rng = new Random();
            int count = options.Length * 50;

            for (int i = 0; i < count; i++)
            {
                yield return options[rng.Next(options.Length)];
            }
        }

        [Theory]
        [MemberData(nameof(ThresholdExceeded_ThrowsException_MemberData))]
        public async Task ThresholdExceeded_ThrowsException(int? maxResponseHeadersLength, int headersLengthEstimate)
        {
            if (UseVersion.Major != 3) return;

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
