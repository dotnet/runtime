// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Connect_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Connect_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ConnectMethod_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("CONNECT"), url) { Version = UseVersion };
                    request.Headers.Host = "foo.com:345";

                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> responseTask = client.SendAsync(TestAsync, request,  HttpCompletionOption.ResponseHeadersRead);

                    await server.AcceptConnectionAsync(async connection =>
                    {
                        // Verify that Host header exist and has same value and URI authority.
                        List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                        string authority = lines[0].Split()[1];
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Host:",StringComparison.InvariantCultureIgnoreCase))
                            {
                                Assert.Equal("Host: foo.com:345", line);
                                break;
                            }
                        }

                        Task serverTask = connection.SendResponseAsync(HttpStatusCode.OK);
                        await TestHelper.WhenAllCompletedOrAnyFailed(responseTask, serverTask).ConfigureAwait(false);

                        using (Stream clientStream = await (await responseTask).Content.ReadAsStreamAsync(TestAsync))
                        {
                            Assert.True(clientStream.CanWrite);
                            Assert.True(clientStream.CanRead);
                            Assert.False(clientStream.CanSeek);

                            TextReader clientReader = new StreamReader(clientStream);
                            TextWriter clientWriter = new StreamWriter(clientStream) { AutoFlush = true };
                            TextWriter serverWriter = connection.Writer;

                            const string helloServer = "hello server";
                            const string helloClient = "hello client";
                            const string goodbyeServer = "goodbye server";
                            const string goodbyeClient = "goodbye client";

                            clientWriter.WriteLine(helloServer);
                            Assert.Equal(helloServer, connection.ReadLine());
                            serverWriter.WriteLine(helloClient);
                            Assert.Equal(helloClient, clientReader.ReadLine());
                            clientWriter.WriteLine(goodbyeServer);
                            Assert.Equal(goodbyeServer, connection.ReadLine());
                            serverWriter.WriteLine(goodbyeClient);
                            Assert.Equal(goodbyeClient, clientReader.ReadLine());
                        }
                    });
                }
            });
        }

        [Fact]
        public async Task ConnectMethod_Fails()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("CONNECT"), url) { Version = UseVersion };
                    request.Headers.Host = "foo.com:345";
                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> responseTask = client.SendAsync(TestAsync, request,  HttpCompletionOption.ResponseHeadersRead);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task<List<string>> serverTask = connection.ReadRequestHeaderAndSendResponseAsync(HttpStatusCode.Forbidden, content: "error");

                        await TestHelper.WhenAllCompletedOrAnyFailed(responseTask, serverTask);
                        HttpResponseMessage response = await responseTask;

                        Assert.True(response.StatusCode ==  HttpStatusCode.Forbidden);
                    });
                }
            });
        }
    }
}
