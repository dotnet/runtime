// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNotBrowser))]
    public sealed class HttpClientHandler_RemoteServerTest : HttpClientHandlerTestBase
    {
        private const string ExpectedContent = "Test content";
        private const string Username = "testuser";
        private const string Password = "password";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public static readonly object[][] Http2Servers = Configuration.Http.Http2Servers;
        public static readonly object[][] Http2NoPushServers = Configuration.Http.Http2NoPushServers;

        // Standard HTTP methods defined in RFC7231: http://tools.ietf.org/html/rfc7231#section-4.3
        //     "GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE"
        public static readonly IEnumerable<object[]> HttpMethods =
            GetMethods("GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE", "CUSTOM1");
        public static readonly IEnumerable<object[]> HttpMethodsThatAllowContent =
            GetMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "CUSTOM1");
        public static readonly IEnumerable<object[]> HttpMethodsThatDontAllowContent =
            GetMethods("HEAD", "TRACE");

        private static bool IsWindows10Version1607OrGreater => PlatformDetection.IsWindows10Version1607OrGreater;

        private static IEnumerable<object[]> GetMethods(params string[] methods)
        {
            foreach (string method in methods)
            {
                foreach (Uri serverUri in Configuration.Http.EchoServerList)
                {
                    yield return new object[] { method, serverUri };
                }
            }
        }

        public HttpClientHandler_RemoteServerTest(ITestOutputHelper output) : base(output)
        {
        }

        [OuterLoop("Uses external servers")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Browser, "UseProxy not supported on Browser")]
        public async Task UseDefaultCredentials_SetToFalseAndServerNeedsAuth_StatusCodeUnauthorized(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Uri uri = Configuration.Http.RemoteHttp11Server.NegotiateAuthUriForDefaultCreds;
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public async Task SendAsync_SimpleGet_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.GetAsync(remoteServer.EchoUri))
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(
                    responseContent,
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task SendAsync_MultipleRequestsReusingSameClient_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                for (int i = 0; i < 3; i++)
                {
                    using (HttpResponseMessage response = await client.GetAsync(remoteServer.EchoUri))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetAsync_ResponseContentAfterClientAndHandlerDispose_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.GetAsync(remoteServer.EchoUri))
            {
                client.Dispose();
                Assert.NotNull(response);
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(responseContent, response.Content.Headers.ContentMD5, false, null);
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
        public async Task GetAsync_ServerNeedsBasicAuthAndSetDefaultCredentials_StatusCodeUnauthorized(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = CredentialCache.DefaultCredentials;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
        public async Task GetAsync_ServerNeedsAuthAndSetCredential_StatusCodeOK(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetAsync_ServerNeedsAuthAndNoCredential_StatusCodeUnauthorized(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(RemoteServersAndHeaderEchoUrisMemberData))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndEmptyValueSent(Configuration.Http.RemoteServer remoteServer, Uri uri)
        {
            if (IsWinHttpHandler && !PlatformDetection.IsWindows10Version1709OrGreater)
            {
                return;
            }

            string name = "X-Cust-Header-NoValue";
            string value = "";
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                _output.WriteLine($"name={name}, value={value}");
                client.DefaultRequestHeaders.Add(name, value);
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersHeaderValuesAndUris))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndValueSent(Configuration.Http.RemoteServer remoteServer, string name, string value, Uri uri)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                _output.WriteLine($"name={name}, value={value}");
                client.DefaultRequestHeaders.Add(name, value);
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersAndHeaderEchoUrisMemberData))]
        public async Task GetAsync_LargeRequestHeader_HeadersAndValuesSent(Configuration.Http.RemoteServer remoteServer, Uri uri)
        {
            // Unfortunately, our remote servers seem to have pretty strict limits (around 16K?)
            // on the total size of the request header.
            // TODO: Figure out how to reconfigure remote endpoints to allow larger request headers,
            // and then increase the limits in this test.

            string headerValue = new string('a', 2048);
            const int headerCount = 6;

            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                for (int i = 0; i < headerCount; i++)
                {
                    client.DefaultRequestHeaders.Add($"Header-{i}", headerValue);
                }

                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();

                    for (int i = 0; i < headerCount; i++)
                    {
                        Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, $"Header-{i}", headerValue));
                    }
                }
            }
        }

        public static IEnumerable<object[]> RemoteServersHeaderValuesAndUris()
        {
            foreach ((Configuration.Http.RemoteServer remoteServer, Uri uri) in RemoteServersAndHeaderEchoUris())
            {
                yield return new object[] { remoteServer, "X-CustomHeader", "x-value", uri };
                yield return new object[] { remoteServer, "MyHeader", "1, 2, 3", uri };

                // Construct a header value with every valid character (except space)
                string allchars = "";
                for (int i = 0x21; i <= 0x7E; i++)
                {
                    allchars = allchars + (char)i;
                }

                // Put a space in the middle so it's not interpreted as insignificant leading/trailing whitespace
                allchars = allchars + " " + allchars;

                yield return new object[] { remoteServer, "All-Valid-Chars-Header", allchars, uri };
            }
        }

        public static IEnumerable<(Configuration.Http.RemoteServer remoteServer, Uri uri)> RemoteServersAndHeaderEchoUris()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                yield return (remoteServer, remoteServer.EchoUri);
                yield return (remoteServer, remoteServer.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1));
            }
        }

        public static IEnumerable<object[]> RemoteServersAndHeaderEchoUrisMemberData() => RemoteServersAndHeaderEchoUris().Select(x => new object[] { x.remoteServer, x.uri });

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetAsync_ResponseHeadersRead_ReadFromEachIterativelyDoesntDeadlock(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                const int NumGets = 5;
                Task<HttpResponseMessage>[] responseTasks = (from _ in Enumerable.Range(0, NumGets)
                                                             select client.GetAsync(remoteServer.EchoUri, HttpCompletionOption.ResponseHeadersRead)).ToArray();
                for (int i = responseTasks.Length - 1; i >= 0; i--) // read backwards to increase likelihood that we wait on a different task than has data available
                {
                    using (HttpResponseMessage response = await responseTasks[i])
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _output.WriteLine(responseContent);
                        TestHelper.VerifyResponseBody(
                            responseContent,
                            response.Content.Headers.ContentMD5,
                            false,
                            null);
                    }
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task SendAsync_HttpRequestMsgResponseHeadersRead_StatusCodeOK(Configuration.Http.RemoteServer remoteServer)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && remoteServer.HttpVersion.Major >= 2)
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, remoteServer.EchoUri) { Version = remoteServer.HttpVersion };
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request, HttpCompletionOption.ResponseHeadersRead))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_CallMethodTwice_StringContent(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                string data = "Test String";
                var content = new StringContent(data, Encoding.UTF8);

                if (PlatformDetection.IsBrowser)
                {
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                    content.Headers.Add("Content-MD5-Skip", "browser");
                }
                else
                {
                    content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                }

                HttpResponseMessage response;
                using (response = await client.PostAsync(remoteServer.VerifyUploadUri, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                // Repeat call.
                content = new StringContent(data, Encoding.UTF8);
                if (PlatformDetection.IsBrowser)
                {
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                    content.Headers.Add("Content-MD5-Skip", "browser");
                }
                else
                {
                    content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                }

                using (response = await client.PostAsync(remoteServer.VerifyUploadUri, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_CallMethod_UnicodeStringContent(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                string data = "\ub4f1\uffc7\u4e82\u67ab4\uc6d4\ud1a0\uc694\uc77c\uffda3\u3155\uc218\uffdb";
                var content = new StringContent(data, Encoding.UTF8);
                if (PlatformDetection.IsBrowser)
                {
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                    content.Headers.Add("Content-MD5-Skip", "browser");
                }
                else
                {
                    content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                }

                using (HttpResponseMessage response = await client.PostAsync(remoteServer.VerifyUploadUri, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(VerifyUploadServersStreamsAndExpectedData))]
        public async Task PostAsync_CallMethod_StreamContent(Configuration.Http.RemoteServer remoteServer, HttpContent content, byte[] expectedData)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                if (PlatformDetection.IsBrowser)
                {
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                    content.Headers.Add("Content-MD5-Skip", "browser");
                }
                else
                {
                    content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(expectedData);
                }

                using (HttpResponseMessage response = await client.PostAsync(remoteServer.VerifyUploadUri, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        private sealed class StreamContentWithSyncAsyncCopy : StreamContent
        {
            private readonly Stream _stream;
            private readonly bool _syncCopy;

            public StreamContentWithSyncAsyncCopy(Stream stream, bool syncCopy) : base(stream)
            {
                _stream = stream;
                _syncCopy = syncCopy;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                if (_syncCopy)
                {
                    try
                    {
                        _stream.CopyTo(stream, 128); // arbitrary size likely to require multiple read/writes
                        return Task.CompletedTask;
                    }
                    catch (Exception exc)
                    {
                        return Task.FromException(exc);
                    }
                }

                return base.SerializeToStreamAsync(stream, context);
            }
        }

        public static IEnumerable<object[]> VerifyUploadServersStreamsAndExpectedData
        {
            get
            {
                foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers) // target server
                    foreach (bool syncCopy in BoolValues) // force the content copy to happen via Read/Write or ReadAsync/WriteAsync
                    {
                        byte[] data = new byte[1234];
                        new Random(42).NextBytes(data);

                        // A MemoryStream
                        {
                            var memStream = new MemoryStream(data, writable: false);
                            yield return new object[] { remoteServer, new StreamContentWithSyncAsyncCopy(memStream, syncCopy: syncCopy), data };
                        }

                        // A multipart content that provides its own stream from CreateContentReadStreamAsync
                        {
                            var mc = new MultipartContent();
                            mc.Add(new ByteArrayContent(data));
                            var memStream = new MemoryStream();
                            mc.CopyToAsync(memStream).GetAwaiter().GetResult();
                            yield return new object[] { remoteServer, mc, memStream.ToArray() };
                        }

                        // A stream that provides the data synchronously and has a known length
                        {
                            var wrappedMemStream = new MemoryStream(data, writable: false);
                            var syncKnownLengthStream = new DelegateStream(
                                canReadFunc: () => wrappedMemStream.CanRead,
                                canSeekFunc: () => wrappedMemStream.CanSeek,
                                lengthFunc: () => wrappedMemStream.Length,
                                positionGetFunc: () => wrappedMemStream.Position,
                                positionSetFunc: p => wrappedMemStream.Position = p,
                                readFunc: (buffer, offset, count) => wrappedMemStream.Read(buffer, offset, count),
                                readAsyncFunc: (buffer, offset, count, token) => wrappedMemStream.ReadAsync(buffer, offset, count, token));
                            yield return new object[] { remoteServer, new StreamContentWithSyncAsyncCopy(syncKnownLengthStream, syncCopy: syncCopy), data };
                        }

                        // A stream that provides the data synchronously and has an unknown length
                        {
                            int syncUnknownLengthStreamOffset = 0;

                            Func<byte[], int, int, int> readFunc = (buffer, offset, count) =>
                            {
                                int bytesRemaining = data.Length - syncUnknownLengthStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, count);
                                Array.Copy(data, syncUnknownLengthStreamOffset, buffer, offset, bytesToCopy);
                                syncUnknownLengthStreamOffset += bytesToCopy;
                                return bytesToCopy;
                            };

                            var syncUnknownLengthStream = new DelegateStream(
                                canReadFunc: () => true,
                                canSeekFunc: () => false,
                                readFunc: readFunc,
                                readAsyncFunc: (buffer, offset, count, token) => Task.FromResult(readFunc(buffer, offset, count)));
                            yield return new object[] { remoteServer, new StreamContentWithSyncAsyncCopy(syncUnknownLengthStream, syncCopy: syncCopy), data };
                        }

                        // A stream that provides the data asynchronously
                        {
                            int asyncStreamOffset = 0, maxDataPerRead = 100;

                            Func<byte[], int, int, int> readFunc = (buffer, offset, count) =>
                            {
                                int bytesRemaining = data.Length - asyncStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, Math.Min(maxDataPerRead, count));
                                Array.Copy(data, asyncStreamOffset, buffer, offset, bytesToCopy);
                                asyncStreamOffset += bytesToCopy;
                                return bytesToCopy;
                            };

                            var asyncStream = new DelegateStream(
                                canReadFunc: () => true,
                                canSeekFunc: () => false,
                                readFunc: readFunc,
                                readAsyncFunc: async (buffer, offset, count, token) =>
                                {
                                    await Task.Delay(1).ConfigureAwait(false);
                                    return readFunc(buffer, offset, count);
                                });
                            yield return new object[] { remoteServer, new StreamContentWithSyncAsyncCopy(asyncStream, syncCopy: syncCopy), data };
                        }

                        // Providing data from a FormUrlEncodedContent's stream
                        {
                            var formContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("key", "val") });
                            yield return new object[] { remoteServer, formContent, Encoding.GetEncoding("iso-8859-1").GetBytes("key=val") };
                        }
                    }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_CallMethod_NullContent(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                using (HttpResponseMessage response = await client.PostAsync(remoteServer.EchoUri, null))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_CallMethod_EmptyContent(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                var content = new StringContent(string.Empty);
                using (HttpResponseMessage response = await client.PostAsync(remoteServer.EchoUri, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);
                }
            }
        }

        public static IEnumerable<object[]> ExpectContinueVersion()
        {
            return
                from expect in new bool?[] {true, false, null}
                from version in new Version[] {new Version(1, 0), new Version(1, 1), new Version(2, 0)}
                select new object[] {expect, version};
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(ExpectContinueVersion))]
        [SkipOnPlatform(TestPlatforms.Browser, "ExpectContinue not supported on Browser")]
        public async Task PostAsync_ExpectContinue_Success(bool? expectContinue, Version version)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && version.Major >= 2)
            {
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, version.Major == 2 ? Configuration.Http.Http2RemoteEchoServer : Configuration.Http.RemoteEchoServer)
                {
                    Content = new StringContent("Test String", Encoding.UTF8),
                    Version = version
                };
                req.Headers.ExpectContinue = expectContinue;

                using (HttpResponseMessage response = await client.SendAsync(TestAsync, req))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    if (!IsWinHttpHandler)
                    {
                        const string ExpectedReqHeader = "\"Expect\": \"100-continue\"";
                        if (expectContinue == true && (version >= new Version(1, 1)))
                        {
                            Assert.Contains(ExpectedReqHeader, await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            Assert.DoesNotContain(ExpectedReqHeader, await response.Content.ReadAsStringAsync());
                        }
                    }
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_Redirect_ResultingGetFormattedCorrectly(Configuration.Http.RemoteServer remoteServer)
        {
            const string ContentString = "This is the content string.";
            var content = new StringContent(ContentString);
            Uri redirectUri = remoteServer.RedirectUriForDestinationUri(
                302,
                remoteServer.EchoUri,
                1);

            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.PostAsync(redirectUri, content))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(ContentString, responseContent);
                Assert.DoesNotContain("Content-Length", responseContent);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_RedirectWith307_LargePayload(Configuration.Http.RemoteServer remoteServer)
        {
            if (remoteServer.HttpVersion == new Version(2, 0))
            {
                // This is occasionally timing out in CI with SocketsHttpHandler and HTTP2, particularly on Linux
                // Likely this is just a very slow test and not a product issue, so just increasing the timeout may be the right fix.
                // Disable until we can investigate further.
                return;
            }

            await PostAsync_Redirect_LargePayload_Helper(remoteServer, 307, true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_RedirectWith302_LargePayload(Configuration.Http.RemoteServer remoteServer)
        {
            await PostAsync_Redirect_LargePayload_Helper(remoteServer, 302, false);
        }

        private async Task PostAsync_Redirect_LargePayload_Helper(Configuration.Http.RemoteServer remoteServer, int statusCode, bool expectRedirectToPost)
        {
            using (var fs = new FileStream(
                Path.Combine(Path.GetTempPath(), Path.GetTempFileName()),
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                0x1000,
                FileOptions.DeleteOnClose))
            {
                string contentString = string.Join("", Enumerable.Repeat("Content", 100000));
                byte[] contentBytes = Encoding.UTF32.GetBytes(contentString);
                fs.Write(contentBytes, 0, contentBytes.Length);
                fs.Flush(flushToDisk: true);
                fs.Position = 0;

                Uri redirectUri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.VerifyUploadUri,
                    hops: 1);
                var content = new StreamContent(fs);

                // Compute MD5 of request body data. This will be verified by the server when it receives the request.
                if (PlatformDetection.IsBrowser)
                {
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                    content.Headers.Add("Content-MD5-Skip", "browser");
                }
                else
                {
                    content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(contentBytes);
                }

                using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
                using (HttpResponseMessage response = await client.PostAsync(redirectUri, content))
                {
                    try
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                    catch
                    {
                        _output.WriteLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
                        throw;
                    }

                    if (expectRedirectToPost)
                    {
                        IEnumerable<string> headerValue = response.Headers.GetValues("X-HttpRequest-Method");
                        Assert.Equal("POST", headerValue.First());
                    }
                }
            }
        }

#if !NETFRAMEWORK
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostAsync_ReuseRequestContent_Success(Configuration.Http.RemoteServer remoteServer)
        {
            const string ContentString = "This is the content string.";
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                var content = new StringContent(ContentString);
                for (int i = 0; i < 2; i++)
                {
                    using (HttpResponseMessage response = await client.PostAsync(remoteServer.EchoUri, content))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains(ContentString, await response.Content.ReadAsStringAsync());
                    }
                }
            }
        }
#endif

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(HttpMethods))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithNoContent_MethodCorrectlySent(
            string method,
            Uri serverUri)
        {
            if (method == "TRACE" && PlatformDetection.IsBrowser)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/53592", TestPlatforms.Browser)]
                return;
            }
            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    serverUri) { Version = UseVersion };

                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyRequestMethod(response, method);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(HttpMethodsThatAllowContent))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithContent_Success(
            string method,
            Uri serverUri)
        {
            if (method == "GET" && PlatformDetection.IsBrowser)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/53591", TestPlatforms.Browser)]
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    serverUri) { Version = UseVersion };
                request.Content = new StringContent(ExpectedContent);
                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyRequestMethod(response, method);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);

                    Assert.Contains($"\"Content-Length\": \"{request.Content.Headers.ContentLength.Value}\"", responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        ExpectedContent);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(HttpMethodsThatDontAllowContent))]
        public async Task SendAsync_SendRequestUsingNoBodyMethodToEchoServerWithContent_NoBodySent(
            string method,
            Uri serverUri)
        {
            if (method == "TRACE" && PlatformDetection.IsBrowser)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/53592", TestPlatforms.Browser)]
                return;
            }
            if (method == "HEAD" && PlatformDetection.IsBrowser)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/53591", TestPlatforms.Browser)]
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    serverUri)
                {
                    Content = new StringContent(ExpectedContent),
                    Version = UseVersion
                };

                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    if (method == "TRACE")
                    {
                        // .NET Framework also allows the HttpWebRequest and HttpClient APIs to send a request using 'TRACE'
                        // verb and a request body. The usual response from a server is "400 Bad Request".
                        // See here for more info: https://github.com/dotnet/runtime/issues/17475
                        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    }
                    else
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        TestHelper.VerifyRequestMethod(response, method);
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Assert.DoesNotContain(ExpectedContent, responseContent);
                    }
                }
            }
        }

        public static IEnumerable<object[]> SendAsync_SendSameRequestMultipleTimesDirectlyOnHandler_Success_MemberData()
        {
            foreach (var server in Configuration.Http.RemoteServers)
            {
                yield return new object[] { server, "12345678910", 0 };
                yield return new object[] { server, "12345678910", 5 };
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(SendAsync_SendSameRequestMultipleTimesDirectlyOnHandler_Success_MemberData))]
        public async Task SendAsync_SendSameRequestMultipleTimesDirectlyOnHandler_Success(Configuration.Http.RemoteServer remoteServer, string stringContent, int startingPosition)
        {
            using (var handler = new HttpMessageInvoker(CreateHttpClientHandler()))
            {
                byte[] byteContent = Encoding.ASCII.GetBytes(stringContent);
                var content = new MemoryStream();
                content.Write(byteContent, 0, byteContent.Length);
                content.Position = startingPosition;
                var request = new HttpRequestMessage(HttpMethod.Post, remoteServer.EchoUri) { Content = new StreamContent(content), Version = UseVersion };

                for (int iter = 0; iter < 2; iter++)
                {
                    using (HttpResponseMessage response = await handler.SendAsync(TestAsync, request, CancellationToken.None))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                        string responseContent = await response.Content.ReadAsStringAsync();

                        Assert.Contains($"\"Content-Length\": \"{request.Content.Headers.ContentLength.Value}\"", responseContent);
                        string bodyContent = System.Text.Json.JsonDocument.Parse(responseContent).RootElement.GetProperty("BodyContent").GetString();
                        Assert.Contains(stringContent.Substring(startingPosition), bodyContent);
                        if (startingPosition != 0)
                        {
                            Assert.DoesNotContain(stringContent.Substring(0, startingPosition), bodyContent);
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> RemoteServersAndRedirectStatusCodes()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                yield return new object[] { remoteServer, 300 };
                yield return new object[] { remoteServer, 301 };
                yield return new object[] { remoteServer, 302 };
                yield return new object[] { remoteServer, 303 };
                yield return new object[] { remoteServer, 307 };
                yield return new object[] { remoteServer, 308 };
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = false;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(statusCode, (int)response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(remoteServer.EchoUri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttps_StatusCodeOK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Uri uri = Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteSecureHttp11Server.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(Configuration.Http.SecureRemoteEchoServer, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpsToHttp_StatusCodeRedirect()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Uri uri = Configuration.Http.RemoteSecureHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteHttp11Server.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);

                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectToUriWithParams_RequestMsgUriSet(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            Uri targetUri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: targetUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Equal(targetUri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [SkipOnPlatform(TestPlatforms.Browser, "MaxConnectionsPerServer not supported on Browser")]
        public async Task GetAsync_MaxAutomaticRedirectionsNServerHops_ThrowsIfTooMany(int maxHops, int hops)
        {
            if (IsWinHttpHandler && !PlatformDetection.IsWindows10Version1703OrGreater)
            {
                // Skip this test if using WinHttpHandler but on a release prior to Windows 10 Creators Update.
                _output.WriteLine("Skipping test due to Windows 10 version prior to Version 1703.");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxAutomaticRedirections = maxHops;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Task<HttpResponseMessage> t = client.GetAsync(Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteHttp11Server.EchoUri,
                    hops: hops));

                if (hops <= maxHops)
                {
                    using (HttpResponseMessage response = await t)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(Configuration.Http.RemoteEchoServer, response.RequestMessage.RequestUri);
                    }
                }
                else
                {
                    if (!IsWinHttpHandler)
                    {
                        using (HttpResponseMessage response = await t)
                        {
                            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                        }
                    }
                    else
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => t);
                    }
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55083", TestPlatforms.Browser)]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectWithRelativeLocation(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1,
                    relative: true);
                _output.WriteLine("Uri: {0}", uri);

                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(remoteServer.EchoUri, response.RequestMessage.RequestUri);
                }
            }
        }

        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [OuterLoop("Uses external servers")]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
        public async Task GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri redirectUri = remoteServer.RedirectUriForCreds(
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }
            }
        }

        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [OuterLoop("Uses external servers")]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
        public async Task HttpClientHandler_CredentialIsNotCredentialCacheAfterRedirect_StatusCodeOK(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri redirectUri = remoteServer.RedirectUriForCreds(
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }

                // Use the same handler to perform get request, authentication should succeed after redirect.
                Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
                using (HttpResponseMessage authResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
            Uri redirectUri = remoteServer.RedirectUriForCreds(
                statusCode: statusCode,
                userName: Username,
                password: Password);
            _output.WriteLine(uri.AbsoluteUri);
            _output.WriteLine(redirectUri.AbsoluteUri);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = credentialCache;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        public async Task DefaultHeaders_SetCredentials_ClearedOnRedirect(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                string credentialString = _credential.UserName + ":" + _credential.Password;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialString);
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.False(TestHelper.JsonMessageContainsKey(responseText, "Authorization"));
                }
            }
        }

        public static IEnumerable<object[]> RemoteServersAndCompressionUris()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                yield return new object[] { remoteServer, remoteServer.GZipUri };

                // Remote deflate endpoint isn't correctly following the deflate protocol.
                //yield return new object[] { remoteServer, remoteServer.DeflateUri };
            }
        }

        [OuterLoop("Uses external servers")]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        [Theory, MemberData(nameof(RemoteServersAndCompressionUris))]
        public async Task GetAsync_SetAutomaticDecompression_ContentDecompressed_GZip(Configuration.Http.RemoteServer remoteServer, Uri uri)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && remoteServer.HttpVersion.Major >= 2)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                using (HttpResponseMessage response = await client.SendAsync(TestAsync, CreateRequest(HttpMethod.Get, uri, remoteServer.HttpVersion)))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        // The remote server endpoint was written to use DeflateStream, which isn't actually a correct
        // implementation of the deflate protocol (the deflate protocol requires the zlib wrapper around
        // deflate).  Until we can get that updated (and deal with previous releases still testing it
        // via a DeflateStream-based implementation), we utilize httpbin.org to help validate behavior.
        [OuterLoop("Uses external servers")]
        [Theory]
        [InlineData("http://httpbin.org/deflate", "\"deflated\": true")]
        [InlineData("https://httpbin.org/deflate", "\"deflated\": true")]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task GetAsync_SetAutomaticDecompression_ContentDecompressed_Deflate(string uri, string expectedContent)
        {
            if (IsWinHttpHandler)
            {
                // WinHttpHandler targets netstandard2.0 and still erroneously uses DeflateStream rather than ZlibStream for deflate.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Assert.Contains(expectedContent, await client.GetStringAsync(uri));
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndCompressionUris))]
        [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
        public async Task GetAsync_SetAutomaticDecompression_HeadersRemoved(Configuration.Http.RemoteServer remoteServer, Uri uri)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && remoteServer.HttpVersion.Major >= 2)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            using (HttpResponseMessage response = await client.SendAsync(TestAsync, CreateRequest(HttpMethod.Get, uri, remoteServer.HttpVersion), HttpCompletionOption.ResponseHeadersRead))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Assert.False(response.Content.Headers.Contains("Content-Encoding"), "Content-Encoding unexpectedly found");
                Assert.False(response.Content.Headers.Contains("Content-Length"), "Content-Length unexpectedly found");
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20IfHttp2Supported(Uri server)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync)
            {
                return;
            }

            // We don't currently have a good way to test whether HTTP/2 is supported without
            // using the same mechanism we're trying to test, so for now we allow both 2.0 and 1.1 responses.
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Version = new Version(2, 0);

            using (HttpClient client = CreateHttpClient())
            {
                // It is generally expected that the test hosts will be trusted, so we don't register a validation
                // callback in the usual case.

                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(
                        response.Version == new Version(2, 0) ||
                        response.Version == new Version(1, 1),
                        "Response version " + response.Version);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(IsWindows10Version1607OrGreater)), MemberData(nameof(Http2NoPushServers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20(Uri server)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync)
            {
                return;
            }

            _output.WriteLine(server.AbsoluteUri.ToString());
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Version = new Version(2, 0);

            using (HttpClient client = CreateHttpClient())
            {
                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(new Version(2, 0), response.Version);
                }
            }
        }
    }
}
