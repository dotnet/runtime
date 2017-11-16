// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientTestBase
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not currently supported on UAP")]
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not currently supported on UAP")]
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
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.MaxResponseHeadersLength = int.MaxValue;
                await client.GetStreamAsync(Configuration.Http.RemoteEchoServer);
                Assert.Throws<InvalidOperationException>(() => handler.MaxResponseHeadersLength = int.MaxValue);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not currently supported on UAP")]
        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(ResponseWithManyHeadersData))]
        public async Task ThresholdExceeded_ThrowsException(string responseHeaders, int maxResponseHeadersLength, bool shouldSucceed)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.MaxResponseHeadersLength = maxResponseHeadersLength;
                    Task<HttpResponseMessage> getAsync = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                    await LoopbackServer.AcceptSocketAsync(server, async (s, serverStream, reader, writer) =>
                    {
                        using (s) using (serverStream) using (reader) using (writer)
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null && !string.IsNullOrEmpty(line)) ;

                            byte[] headerData = Encoding.ASCII.GetBytes(responseHeaders);
                            serverStream.Write(headerData, 0, headerData.Length);
                        }

                        if (shouldSucceed)
                        {
                            (await getAsync).Dispose();
                        }
                        else
                        {
                            await Assert.ThrowsAsync<HttpRequestException>(() => getAsync);
                        }
                        
                        return null;
                    });
                }
            });
        }

        public static IEnumerable<object[]> ResponseWithManyHeadersData
        {
            get
            {
                // Success case: response headers of size 1023 bytes (less than 1024 bytes max).
                {
                    yield return new object[] { GenerateLargeResponseHeaders(1023), 1, true };
                }

                // Success case: response headers of size 1024 bytes (equal to 1024 bytes max).
                {
                    yield return new object[] { GenerateLargeResponseHeaders(1024), 1, true };
                }

                // Failure case: response headers of size 1025 (greater than 1024 bytes max).
                {
                    yield return new object[] { GenerateLargeResponseHeaders(1025), 1, false };
                }
            }
        }

        private static string GenerateLargeResponseHeaders(int responseHeadersSizeInBytes)
        {
            // This helper method only supports generating sizes of 1023, 1024, or 1025 bytes.
            // These are the only sizes needed to support the above tests.
            Assert.InRange(responseHeadersSizeInBytes, 1023, 1025);

            string statusHeader = "HTTP/1.1 200 OK\r\n";
            string contentFooter = "Content-Length: 0\r\n\r\n";

            var buffer = new StringBuilder();
            buffer.Append(statusHeader);
            for (int i = 0; i < 24; i++)
            {
                buffer.Append($"Custom-{i:D4}: 1234567890123456789012345\r\n");
            }

            if (responseHeadersSizeInBytes == 1023)
            {
                buffer.Append($"Custom-1023: 1234567890\r\n");
            }
            else if (responseHeadersSizeInBytes == 1024)
            {
                buffer.Append($"Custom-1024: 12345678901\r\n");
            }
            else
            {
                Assert.Equal(1025, responseHeadersSizeInBytes);
                buffer.Append($"Custom-1025: 123456789012\r\n");
            }

            buffer.Append(contentFooter);

            string response = buffer.ToString();            
            Assert.Equal(responseHeadersSizeInBytes, response.Length);

            return response;
        }
    }
}
