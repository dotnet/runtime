// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_MaxResponseHeadersLength_Test : RemoteExecutorTestBase
    {
        [Fact]
        public void Default_MaxResponseHeadersLength()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Equal(64 * 1024, handler.MaxResponseHeadersLength);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InvalidValue_ThrowsException(int invalidValue)
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = invalidValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(65 * 1024)]
        [InlineData(int.MaxValue)]
        public void ValidValue_SetGet_Roundtrips(int validValue)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.MaxResponseHeadersLength = validValue;
                Assert.Equal(validValue, handler.MaxResponseHeadersLength);
            }
        }

        [Fact]
        public async Task SetAfterUse_Throws()
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.MaxResponseHeadersLength = int.MaxValue;
                await client.GetStreamAsync(Configuration.Http.RemoteEchoServer);
                Assert.Throws<InvalidOperationException>(() => handler.MaxResponseHeadersLength = int.MaxValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n", 37, false)]
        [InlineData("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n", 38, true)]
        [InlineData("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n", 39, true)]
        public async Task ThresholdExceeded_ThrowsException(string responseHeaders, int maxResponseHeadersLength, bool shouldSucceed)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (var handler = new HttpClientHandler() { MaxResponseHeadersLength = maxResponseHeadersLength })
                using (var client = new HttpClient(handler))
                {
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

    }
}
