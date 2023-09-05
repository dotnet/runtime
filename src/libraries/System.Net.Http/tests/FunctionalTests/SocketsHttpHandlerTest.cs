// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Asynchrony_Test : HttpClientHandler_Asynchrony_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Asynchrony_Test(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Relies on finalization")]
        [Fact]
        public async Task ReadAheadTaskOnScavenge_ExceptionsAreObserved()
        {
            bool seenUnobservedExceptions = false;

            EventHandler<UnobservedTaskExceptionEventArgs> eventHandler = (_, e) =>
            {
                if (e.Exception.InnerException?.Message == nameof(ReadAheadTaskOnScavenge_ExceptionsAreObserved))
                {
                    seenUnobservedExceptions = true;
                }
            };

            TaskScheduler.UnobservedTaskException += eventHandler;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    await MakeARequestWithoutDisposingTheHandlerAsync();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(1000);
                }
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= eventHandler;
            }

            Assert.False(seenUnobservedExceptions);

            static async Task MakeARequestWithoutDisposingTheHandlerAsync()
            {
                var cts = new CancellationTokenSource();
                var requestCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                var handler = new SocketsHttpHandler();
                handler.ConnectCallback = async (_, _) =>
                {
                    cts.Cancel();
                    await requestCompleted.Task;

                    Task completedWhenFinalized = new SetOnFinalized().CompletedWhenFinalized.Task;

                    return new DelegateDelegatingStream(Stream.Null)
                    {
                        ReadAsyncMemoryFunc = async (_, _) =>
                        {
                            await completedWhenFinalized.WaitAsync(TestHelper.PassingTestTimeout);

                            throw new Exception(nameof(ReadAheadTaskOnScavenge_ExceptionsAreObserved));
                        }
                    };
                };

                handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1);

                var client = new HttpClient(handler);

                await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetStringAsync("http://foo", cts.Token));

                requestCompleted.SetResult();
            }
        }

        [Fact]
        public async Task ExecutionContext_Suppressed_Success()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                uri => Task.Run(() =>
                {
                    using (ExecutionContext.SuppressFlow())
                    using (HttpClient client = CreateHttpClient())
                    {
                        client.GetStringAsync(uri).GetAwaiter().GetResult();
                    }
                }),
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync();
                });
        }

        [OuterLoop("Relies on finalization")]
        [Fact]
        public async Task ExecutionContext_HttpConnectionLifetimeDoesntKeepContextAlive()
        {
            var clientCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        (Task completedWhenFinalized, Task getRequest) = MakeHttpRequestWithTcsSetOnFinalizationInAsyncLocal(client, uri);
                        await getRequest;

                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        await completedWhenFinalized.WaitAsync(TestHelper.PassingTestTimeout);
                    }
                }
                finally
                {
                    clientCompleted.SetResult();
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAndSendResponseAsync();
                    await clientCompleted.Task;
                });
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // avoid JIT extending lifetime of the finalizable object
        private static (Task completedOnFinalized, Task getRequest) MakeHttpRequestWithTcsSetOnFinalizationInAsyncLocal(HttpClient client, Uri uri)
        {
            // Put something in ExecutionContext, start the HTTP request, then undo the EC change.
            var al = new AsyncLocal<SetOnFinalized>() { Value = new SetOnFinalized() };
            TaskCompletionSource tcs = al.Value.CompletedWhenFinalized;
            Task t = client.GetStringAsync(uri);
            al.Value = null;

            // Return a task that will complete when the SetOnFinalized is finalized,
            // as well as a task to wait on for the get request; for the get request,
            // we return a continuation to avoid any test-altering issues related to
            // the state machine holding onto stuff.
            t = t.ContinueWith(p => p.GetAwaiter().GetResult());
            return (tcs.Task, t);
        }

        private sealed class SetOnFinalized
        {
            public readonly TaskCompletionSource CompletedWhenFinalized = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ~SetOnFinalized() => CompletedWhenFinalized.SetResult();
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_HttpProtocolTests : HttpProtocolTests
    {
        public SocketsHttpHandler_HttpProtocolTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task DefaultRequestHeaders_SentUnparsed()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5"); // validation would add spaces
                    client.DefaultRequestHeaders.TryAddWithoutValidation("From", "invalidemail"); // would fail to parse if validated

                    var m = new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion };
                    (await client.SendAsync(TestAsync, m)).Dispose();
                }
            }, async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync();
                Assert.Contains(headers, header => header.Contains("Accept-Language: en-US,en;q=0.5"));
                Assert.Contains(headers, header => header.Contains("From: invalidemail"));
            });
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        public SocketsHttpHandler_HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_DiagnosticsTest_Http11 : DiagnosticsTest
    {
        public SocketsHttpHandler_DiagnosticsTest_Http11(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_DiagnosticsTest_Http2 : DiagnosticsTest
    {
        public SocketsHttpHandler_DiagnosticsTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public sealed class SocketsHttpHandler_HttpClient_SelectedSites_Test : HttpClient_SelectedSites_Test
    {
        public SocketsHttpHandler_HttpClient_SelectedSites_Test(ITestOutputHelper output) : base(output) { }
    }

#if !TARGETS_BROWSER
    public sealed class SocketsHttpHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        public SocketsHttpHandler_HttpClientEKUTest(ITestOutputHelper output) : base(output) { }
    }
#endif

    [SkipOnPlatform(TestPlatforms.Browser, "AutomaticDecompression not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Certificates are not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        public SocketsHttpHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Certificates are not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Proxy is not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        public SocketsHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http11_Test : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http11_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http2_Test : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http2_Test(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "MaxConnectionsPerServer not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        public SocketsHttpHandler_HttpClientHandler_MaxConnectionsPerServer_Test(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void AppContextSetData_SetDefaultMaxConnectionsPerServer(bool asInt)
        {
            RemoteExecutor.Invoke(static (asInt) =>
            {
                const int testValue = 123;
                object data = asInt == Boolean.TrueString ? testValue : testValue.ToString();
                AppContext.SetData("System.Net.SocketsHttpHandler.MaxConnectionsPerServer", data);
                var handler = new HttpClientHandler();
                Assert.Equal(testValue, handler.MaxConnectionsPerServer);
            }, asInt.ToString()).Dispose();
        }

        [OuterLoop("Incurs a small delay")]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task SmallConnectionLifetimeWithMaxConnections_PendingRequestUsesDifferentConnection(int lifetimeMilliseconds)
        {
            using (var handler = new SocketsHttpHandler())
            {
                handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(lifetimeMilliseconds);
                handler.MaxConnectionsPerServer = 1;

                using (HttpClient client = CreateHttpClient(handler))
                {
                    await LoopbackServer.CreateServerAsync(async (server, uri) =>
                    {
                        Task<string> request1 = client.GetStringAsync(uri);
                        Task<string> request2 = client.GetStringAsync(uri);

                        await server.AcceptConnectionAsync(async connection =>
                        {
                            Task secondResponse = server.AcceptConnectionAsync(connection2 =>
                                connection2.ReadRequestHeaderAndSendCustomResponseAsync(LoopbackServer.GetConnectionCloseResponse()));

                            // Wait a small amount of time before sending the first response, so the connection lifetime will expire.
                            Debug.Assert(lifetimeMilliseconds < 100);
                            await Task.Delay(1000);

                            // Second request should not have completed yet, as we haven't completed the first yet.
                            Assert.False(request2.IsCompleted);
                            Assert.False(secondResponse.IsCompleted);

                            // Send the first response and wait for the first request to complete.
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                            await request1;

                            // Now the second request should complete.
                            await secondResponse.WaitAsync(TestHelper.PassingTestTimeout);
                        });
                    });
                }
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Certificates are not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        public SocketsHttpHandler_HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "ResponseDrainTimeout is not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_ResponseDrain_Test : HttpClientHandler_ResponseDrain_Test
    {
        protected override void SetResponseDrainTimeout(HttpClientHandler handler, TimeSpan time)
        {
            SocketsHttpHandler s = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            Assert.NotNull(s);
            s.ResponseDrainTimeout = time;
        }

        public SocketsHttpHandler_HttpClientHandler_ResponseDrain_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void MaxResponseDrainSize_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);

                handler.MaxResponseDrainSize = 0;
                Assert.Equal(0, handler.MaxResponseDrainSize);

                handler.MaxResponseDrainSize = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxResponseDrainSize);
            }
        }

        [Fact]
        public void MaxResponseDrainSize_InvalidArgument_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseDrainSize = -1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseDrainSize = int.MinValue);

                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);
            }
        }

        [Fact]
        public void MaxResponseDrainSize_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.MaxResponseDrainSize = 1;
                _ = client.GetAsync($"http://{Guid.NewGuid():N}"); // ignoring failure
                Assert.Equal(1, handler.MaxResponseDrainSize);
                Assert.Throws<InvalidOperationException>(() => handler.MaxResponseDrainSize = 1);
            }
        }

        [Fact]
        public void ResponseDrainTimeout_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);

                handler.ResponseDrainTimeout = TimeSpan.Zero;
                Assert.Equal(TimeSpan.Zero, handler.ResponseDrainTimeout);

                handler.ResponseDrainTimeout = TimeSpan.FromTicks(int.MaxValue);
                Assert.Equal(TimeSpan.FromTicks(int.MaxValue), handler.ResponseDrainTimeout);
            }
        }

        [Fact]
        public void MaxResponseDraiTime_InvalidArgument_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(-1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.MaxValue);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(int.MaxValue));

                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);
            }
        }

        [Fact]
        public void ResponseDrainTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ResponseDrainTimeout = TimeSpan.FromSeconds(42);
                _ = client.GetAsync($"http://{Guid.NewGuid():N}"); // ignoring failure
                Assert.Equal(TimeSpan.FromSeconds(42), handler.ResponseDrainTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(42));
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 3, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 3, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 13, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsUnderMaxDrainSizeAndReusesConnection(int totalSize, int readSize, int maxDrainSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = maxDrainSize;
                    handler.ResponseDrainTimeout = Timeout.InfiniteTimeSpan;

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);

                        // Read part but not all of response
                        Stream responseStream = await response1.Content.ReadAsStreamAsync(TestAsync);
                        await ReadToByteCount(responseStream, readSize);

                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on the same connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    string response = LoopbackServer.GetContentModeResponse(mode, content);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        server.ListenSocket.Close(); // Shut down the listen socket so attempts at additional connections would fail on the client
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        [OuterLoop]
        [Theory]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeLargerThanMaxDrainSize_KillsConnection(int totalSize, int maxDrainSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = maxDrainSize;
                    handler.ResponseDrainTimeout = Timeout.InfiniteTimeSpan;

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);
                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on a new connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            await connection.WriteStringAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: false));
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.

                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: true));
                    });
                });
        }

        [OuterLoop]
        [Theory]
        [InlineData(LoopbackServer.ContentMode.ContentLength)]
        [InlineData(LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DrainTakesLongerThanTimeout_KillsConnection(LoopbackServer.ContentMode mode)
        {
            const int ContentLength = 10_000;

            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = int.MaxValue;
                    handler.ResponseDrainTimeout = TimeSpan.FromMilliseconds(1);

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;

                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, ContentLength, mode);
                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on a new connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, ContentLength, mode);
                        Assert.Equal(ContentLength, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', ContentLength);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        string response = LoopbackServer.GetContentModeResponse(mode, content, connectionClose: false);
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            // Write out only part of the response
                            await connection.WriteStringAsync(response.Substring(0, response.Length / 2));
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.

                        response = LoopbackServer.GetContentModeResponse(mode, content, connectionClose: true);
                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(response);
                    });
                });
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_PostScenarioTest : PostScenarioTest
    {
        public SocketsHttpHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeTargetStream_ThrowsObjectDisposedException(bool knownLength)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        Task t = client.PostAsync(uri, new DisposeStreamWhileCopyingContent(knownLength));
                        Assert.IsType<ObjectDisposedException>((await Assert.ThrowsAsync<HttpRequestException>(() => t)).InnerException);
                    }
                }
                finally
                {
                    tcs.SetResult();
                }
            }, server => tcs.Task);
        }

        private sealed class DisposeStreamWhileCopyingContent : HttpContent
        {
            private readonly bool _knownLength;

            public DisposeStreamWhileCopyingContent(bool knownLength) => _knownLength = knownLength;

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                await stream.WriteAsync(new byte[42], 0, 42);
                stream.Dispose();
            }

            protected override bool TryComputeLength(out long length)
            {
                if (_knownLength)
                {
                    length = 42;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }
            }
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNotBrowser))]
    public sealed class SocketsHttpHandler_ResponseStreamTest : ResponseStreamTest
    {
        public SocketsHttpHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
    public sealed class SocketsHttpHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        public SocketsHttpHandler_HttpClientHandler_SslProtocols_Test(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "UseProxy not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Proxy_Https_Succeeds(bool secureUri)
        {
            var releaseServer = new TaskCompletionSource();
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                bool validationCalled = false;
                using SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);

                handler.Proxy = new UseSpecifiedUriWebProxy(uri, new NetworkCredential("abc", "password"));
                handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, error) =>
                {
                    validationCalled = true;
                    return true;
                };

                using (HttpClient client = CreateHttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(secureUri ? "https://foo.bar/" : "http://foo.bar/");
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(validationCalled);

                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                await connection.ReadRequestHeaderAndSendResponseAsync();
                if (secureUri)
                {
                    // client will send CONNECT and if that succeeds it will negotiate TLS

                    var sslConnection = await LoopbackServer.Connection.CreateAsync(null, connection.Stream, new LoopbackServer.Options { UseSsl = true });
                    await sslConnection.ReadRequestHeaderAndSendResponseAsync();
                }
            }),
            new LoopbackServer.Options { UseSsl = true });
        }
    }

    public abstract class SocketsHttpHandler_TrailingHeaders_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }

        protected static byte[] DataBytes = "data"u8.ToArray();

        protected static readonly IList<HttpHeaderData> TrailingHeaders = new HttpHeaderData[] {
            new HttpHeaderData("MyCoolTrailerHeader", "amazingtrailer"),
            new HttpHeaderData("EmptyHeader", ""),
            new HttpHeaderData("Accept-Encoding", "identity,gzip"),
            new HttpHeaderData("Hello", "World") };

        protected static Frame MakeDataFrame(int streamId, byte[] data, bool endStream = false) =>
            new DataFrame(data, (endStream ? FrameFlags.EndStream : FrameFlags.None), 0, streamId);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/54156", TestPlatforms.Browser)]
    public class SocketsHttpHandler_Http1_TrailingHeaders_Test : SocketsHttpHandler_TrailingHeaders_Test
    {
        public SocketsHttpHandler_Http1_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsyncDefaultCompletionOption_TrailingHeaders_Available(bool includeTrailerHeader)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            LoopbackServer.CorsHeaders +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            (includeTrailerHeader ? "Trailer: MyCoolTrailerHeader, Hello\r\n" : "") +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "Accept-encoding: identity,gzip\r\n" +
                            "Hello: World\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));

                        // Check the Trailer header.
                        if (includeTrailerHeader)
                        {
                            Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));
                            Assert.Contains("Hello", response.Headers.GetValues("Trailer"));
                        }

                        Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                        Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
                        Assert.Contains("identity,gzip", response.TrailingHeaders.GetValues("Accept-encoding"));

                        string data = await response.Content.ReadAsStringAsync();
                        Assert.Contains("data", data);
                        // Trailers should not be part of the content data.
                        Assert.DoesNotContain("MyCoolTrailerHeader", data);
                        Assert.DoesNotContain("amazingtrailer", data);
                        Assert.DoesNotContain("Hello", data);
                        Assert.DoesNotContain("World", data);
                    }
                }
            });
        }

        [Fact]
        public async Task GetAsyncResponseHeadersReadOption_TrailingHeaders_Available()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            LoopbackServer.CorsHeaders +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            "Trailer: MyCoolTrailerHeader\r\n" +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "Hello: World\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));
                        Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));

                        // Pending read on the response content.
                        var trailingHeaders = response.TrailingHeaders;
                        Assert.Empty(trailingHeaders);

                        Stream stream = await response.Content.ReadAsStreamAsync(TestAsync);
                        Byte[] data = new Byte[100];
                        // Read some data, preferably whole body.
                        int readBytes = await stream.ReadAsync(data, 0, 4);

                        // Intermediate test - haven't reached stream EOF yet.
                        Assert.Empty(response.TrailingHeaders);
                        if (readBytes == 4)
                        {
                            // If we consumed whole content, check content.
                            Assert.Contains("data", System.Text.Encoding.Default.GetString(data));
                        }

                        // Read data until EOF is reached
                        while (stream.Read(data, 0, data.Length) != 0)
                            ;

                        Assert.Same(trailingHeaders, response.TrailingHeaders);
                        Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                        Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
                    }
                }
            });
        }

        [Theory]
        [InlineData(1024, 1023)]
        [InlineData(1024, 1024)]
        [InlineData(1024, 1025)]
        [InlineData(1024 * 1024, 1024 * 1024)]
        [InlineData(1024 * 1024, 1024 * 1024 + 1)]
        public async Task GetAsync_TrailingHeadersLimitExceeded_Throws(int maxResponseHeadersLength, int responseHeadersLength)
        {
            Assert.Equal(0, maxResponseHeadersLength % 1024);

            var sb = new StringBuilder()
                .Append("HTTP/1.1 200 OK\r\n")
                .Append("Connection: close\r\n")
                .Append("Transfer-Encoding: chunked\r\n\r\n");

            // Both regular and trailing response headers count against the same length limit
            int headerBytesRemaining = responseHeadersLength - sb.Length;

            sb.Append("0\r\n"); // chunked content

            const string HeaderLine = "Test: value";

            while (headerBytesRemaining > HeaderLine.Length * 2)
            {
                sb.Append(HeaderLine).Append("\r\n");
                headerBytesRemaining -= (HeaderLine.Length + 2);
            }

            sb.Append("Test: ");
            sb.Append('a', headerBytesRemaining - "Test: \r\n\r\n".Length);
            sb.Append("\r\n");

            sb.Append("\r\n");

            string response = sb.ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);

                    handler.MaxResponseHeadersLength = maxResponseHeadersLength / 1024;

                    if (responseHeadersLength > maxResponseHeadersLength)
                    {
                        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                        Assert.Contains("exceeded", exception.Message);
                    }
                    else
                    {
                        (await client.GetAsync(uri)).Dispose();
                    }
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(response);
                    }
                    catch { }
                });
        }

        [Theory]
        [InlineData("Age", "1")]
        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test dummy authorisation header.")]
        [InlineData("Authorization", "Basic YWxhZGRpbjpvcGVuc2VzYW1l")]
        [InlineData("Cache-Control", "no-cache")]
        [InlineData("Content-Encoding", "gzip")]
        [InlineData("Content-Length", "22")]
        [InlineData("Content-type", "foo/bar")]
        [InlineData("Content-Range", "bytes 200-1000/67589")]
        [InlineData("Date", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Expect", "100-continue")]
        [InlineData("Expires", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Host", "foo")]
        [InlineData("If-Match", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-Modified-Since", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-None-Match", "*")]
        [InlineData("If-Range", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-Unmodified-Since", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Location", "/index.html")]
        [InlineData("Max-Forwards", "2")]
        [InlineData("Pragma", "no-cache")]
        [InlineData("Range", "5/10")]
        [InlineData("Retry-After", "20")]
        [InlineData("Set-Cookie", "foo=bar")]
        [InlineData("TE", "boo")]
        [InlineData("Transfer-Encoding", "chunked")]
        [InlineData("Transfer-Encoding", "gzip")]
        [InlineData("Vary", "*")]
        [InlineData("Warning", "300 - \"Be Warned!\"")]
        public async Task GetAsync_ForbiddenTrailingHeaders_Ignores(string name, string value)
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                    Assert.False(response.TrailingHeaders.TryGetValues(name, out IEnumerable<string> values));
                    Assert.Contains("Loopback", response.TrailingHeaders.GetValues("Server"));
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(
                "HTTP/1.1 200 OK\r\n" +
                LoopbackServer.CorsHeaders +
                "Connection: close\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                $"Trailer: Set-Cookie, MyCoolTrailerHeader, {name}, Hello\r\n" +
                "\r\n" +
                "4\r\n" +
                "data\r\n" +
                "0\r\n" +
                "Set-Cookie: yummy\r\n" +
                "MyCoolTrailerHeader: amazingtrailer\r\n" +
                $"{name}: {value}\r\n" +
                "Server: Loopback\r\n" +
                $"{name}: {value}\r\n" +
                "\r\n"));
        }

        [Fact]
        public async Task GetAsync_NoTrailingHeaders_EmptyCollection()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            LoopbackServer.CorsHeaders +
                            "Transfer-Encoding: chunked\r\n" +
                            "Trailer: MyCoolTrailerHeader\r\n" +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));

                        Assert.NotNull(response.TrailingHeaders);
                        Assert.Equal(0, response.TrailingHeaders.Count());
                        Assert.Same(response.TrailingHeaders, response.TrailingHeaders);
                    }
                }
            });
        }
    }

    // TODO: make generic to support HTTP/2 and HTTP/3.
    public sealed class SocketsHttpHandler_Http2_TrailingHeaders_Test : SocketsHttpHandler_TrailingHeaders_Test
    {
        public SocketsHttpHandler_Http2_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
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
                Assert.NotNull(response.TrailingHeaders);
                Assert.Equal(0, response.TrailingHeaders.Count());
            }
        }

        [InlineData(false)]
        [InlineData(true)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
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
                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingPseudoHeadersThrow()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));
                // Additional trailing header frame with pseudo-headers again..
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader: false, headers: TrailingHeaders, endStream: true);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        [InlineData(false)]
        [InlineData(true)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
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
                Assert.Empty(response.TrailingHeaders);

                Stream stream = await response.Content.ReadAsStreamAsync(TestAsync);
                Byte[] data = new Byte[100];
                await stream.ReadAsync(data, 0, data.Length);

                // Intermediate test - haven't reached stream EOF yet.
                Assert.Empty(response.TrailingHeaders);

                // Finish data stream and write out trailing headers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));
                await connection.SendResponseHeadersAsync(streamId, endStream: true, isTrailingHeader: true, headers: TrailingHeaders);

                // Read data until EOF is reached
                while (stream.Read(data, 0, data.Length) != 0) ;

                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
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
                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
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
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }
    }

    public sealed class SocketsHttpHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public SocketsHttpHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public SocketsHttpHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public SocketsHttpHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public SocketsHttpHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool SupportsIdna => true;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandlerTest_RequestRetry : HttpClientHandlerTest_RequestRetry
    {
        public SocketsHttpHandlerTest_RequestRetry(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "UseCookies is not supported on Browser")]
    public sealed class SocketsHttpHandlerTest_Cookies : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "UseCookies is not supported on Browser")]
    public sealed class SocketsHttpHandlerTest_Cookies_Http11 : HttpClientHandlerTest_Cookies_Http11
    {
        public SocketsHttpHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Http11_Cancellation_Test : SocketsHttpHandler_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Http11_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ConnectTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.ConnectTimeout);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void ConnectTimeout_InvalidValues(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        public void ConnectTimeout_ValidValues_Roundtrip(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms);
                Assert.Equal(TimeSpan.FromMilliseconds(ms), handler.ConnectTimeout);
            }
        }

        [Fact]
        public void ConnectTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
                _ = client.GetAsync($"http://{Guid.NewGuid():N}"); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), handler.ConnectTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(1));
            }
        }

        [Fact]
        public void Expect100ContinueTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(1), handler.Expect100ContinueTimeout);
            }
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void Expect100ContinueTimeout_InvalidValues(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(ms));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        public void Expect100ContinueTimeout_ValidValues_Roundtrip(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(ms);
                Assert.Equal(TimeSpan.FromMilliseconds(ms), handler.Expect100ContinueTimeout);
            }
        }

        [Fact]
        public void Expect100ContinueTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
                _ = client.GetAsync($"http://{Guid.NewGuid():N}"); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), handler.Expect100ContinueTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(1));
            }
        }
    }

    public abstract class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        public SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ServerAdvertisedMaxHeaderListSize_IsHonoredByClient()
        {
            if (UseVersion.Major == 1)
            {
                // HTTP/1.X doesn't have a concept of SETTINGS_MAX_HEADER_LIST_SIZE.
                return;
            }

            // On HTTP/3 there is no synchronization between regular requests and the acknowledgement of the SETTINGS frame.
            // Retry the test with increasing delays to give the client connection a chance to observe the settings.
            int retry = 0;
            await RetryHelper.ExecuteAsync(async () =>
            {
                retry++;

                const int Limit = 10_000;

                using HttpClientHandler handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);

                // We want to test that the client remembered the setting it received from the previous connection.
                // To do this, we trick the client into using the same HttpConnectionPool for both server connections.
                // We only have control over the ConnectCallback on HTTP/2.
                bool fakeRequestHost = UseVersion.Major == 2;
                Uri lastServerUri = null;

                GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = async (context, ct) =>
                {
                    Assert.Equal("foo", context.DnsEndPoint.Host);

                    return await DefaultConnectCallback(new DnsEndPoint(lastServerUri.IdnHost, lastServerUri.Port), ct);
                };

                TaskCompletionSource waitingForLastRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);

                await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
                {
                    if (fakeRequestHost)
                    {
                        lastServerUri = uri;
                        uri = new UriBuilder(uri) { Host = "foo", Port = 42 }.Uri;
                    }

                    // Send a dummy request to ensure the SETTINGS frame has been received.
                    Assert.Equal("Hello world", await client.GetStringAsync(uri));

                    if (retry > 1)
                    {
                        // Give the client HTTP/3 connection a chance to observe the SETTINGS frame.
                        await Task.Delay(100 * retry);
                    }

                    HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    request.Headers.Add("Foo", new string('a', Limit));

                    Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                    Assert.Contains(Limit.ToString(), ex.Message);

                    request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    for (int i = 0; i < Limit / 40; i++)
                    {
                        request.Headers.Add($"Foo-{i}", "");
                    }

                    ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                    Assert.Contains(Limit.ToString(), ex.Message);

                    await waitingForLastRequest.Task.WaitAsync(TimeSpan.FromSeconds(10));

                    // Ensure that the connection is still usable for requests that don't hit the limit.
                    Assert.Equal("Hello world", await client.GetStringAsync(uri));
                },
                async server =>
                {
                    var setting = new SettingsEntry { SettingId = SettingId.MaxHeaderListSize, Value = Limit };

                    await using GenericLoopbackConnection connection = UseVersion.Major == 2
                        ? await ((Http2LoopbackServer)server).EstablishConnectionAsync(setting)
                        : await ((Http3LoopbackServer)server).EstablishConnectionAsync(setting);

                    await connection.ReadRequestDataAsync();
                    await connection.SendResponseAsync(content: "Hello world");

                    // On HTTP/3, the client will establish a request stream before buffering the headers.
                    // Swallow two streams to account for the client creating and closing them before reporting the error.
                    if (connection is Http3LoopbackConnection http3Connection)
                    {
                        await http3Connection.AcceptRequestStreamAsync().WaitAsync(TimeSpan.FromSeconds(10));
                        await http3Connection.AcceptRequestStreamAsync().WaitAsync(TimeSpan.FromSeconds(10));
                    }

                    waitingForLastRequest.SetResult();

                    // HandleRequestAsync will close the connection
                    await connection.HandleRequestAsync(content: "Hello world");

                    if (UseVersion.Major == 3)
                    {
                        await ((Http3LoopbackConnection)connection).ShutdownAsync();
                    }
                });

                if (fakeRequestHost)
                {
                    await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
                    {
                        lastServerUri = uri;
                        uri = new UriBuilder(uri) { Host = "foo", Port = 42 }.Uri;

                        HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                        request.Headers.Add("Foo", new string('a', Limit));

                        Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                        Assert.Contains(Limit.ToString(), ex.Message);

                        // Ensure that the connection is still usable for requests that don't hit the limit.
                        Assert.Equal("Hello world", await client.GetStringAsync(uri));
                    },
                    async server =>
                    {
                        await server.HandleRequestAsync(content: "Hello world");
                    });
                }
            }, maxAttempts: UseVersion.Major == 3 ? 5 : 1);
        }
    }

    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http11 : SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength
    {
        public SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http11(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(null, 63 * 1024)]
        [InlineData(null, 65 * 1024)]
        [InlineData(1, 100)]
        [InlineData(1, 1024)]
        public async Task LargeStatusLine_ThrowsException(int? maxResponseHeadersLength, int statusLineLengthEstimate)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();

                if (maxResponseHeadersLength.HasValue)
                {
                    handler.MaxResponseHeadersLength = maxResponseHeadersLength.Value;
                }

                using HttpClient client = CreateHttpClient(handler);

                if (statusLineLengthEstimate < handler.MaxResponseHeadersLength * 1024L)
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
                try
                {
                    await server.AcceptConnectionSendCustomResponseAndCloseAsync($"HTTP/1.1 200 OK{new string('a', statusLineLengthEstimate)}\r\n\r\n");
                }
                catch { }
            });
        }

        public static IEnumerable<object[]> TripleBoolValues() =>
            from trailing in BoolValues
            from async in BoolValues
            from lineFolds in BoolValues
            select new object[] { trailing, async, lineFolds };

        private delegate int StreamReadSpanDelegate(Span<byte> buffer);

        [Theory]
        [MemberData(nameof(TripleBoolValues))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/77474", TestPlatforms.Android)]
        public async Task LargeHeaders_TrickledOverTime_ProcessedEfficiently(bool trailingHeaders, bool async, bool lineFolds)
        {
            Memory<byte> responsePrefix = Encoding.ASCII.GetBytes(trailingHeaders
                ? "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n0\r\nLong-Header: "
                : "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nLong-Header: ");

            bool streamDisposed = false;
            bool responseComplete = false;
            int readCount = 0;
            int fastFillLength = 64 * 1024 * 1024; // 64 MB

            StreamReadSpanDelegate readFunc = buffer =>
            {
                if (streamDisposed)
                {
                    throw new ObjectDisposedException("Foo");
                }

                if (responseComplete)
                {
                    return 0;
                }

                if (!responsePrefix.IsEmpty)
                {
                    int toCopy = Math.Min(responsePrefix.Length, buffer.Length);
                    responsePrefix.Span.Slice(0, toCopy).CopyTo(buffer);
                    responsePrefix = responsePrefix.Slice(toCopy);
                    return toCopy;
                }

                if (fastFillLength > 0)
                {
                    int toFill = Math.Min(fastFillLength, buffer.Length);
                    buffer.Slice(0, toFill).Fill((byte)'a');
                    fastFillLength -= toFill;
                    if (lineFolds)
                    {
                        for (int i = 0; i < toFill / 10; i++)
                        {
                            buffer[i * 10 + 8] = (byte)'\n';
                            buffer[i * 10 + 9] = (byte)' ';
                        }
                    }
                    return toFill;
                }

                if (++readCount < 500_000)
                {
                    // Slowly trickle data over 500 thousand read calls.
                    // If the implementation scans the whole buffer after every read, it will have to sift through 32 TB of data.
                    // As that is not achievable on current hardware within the PassingTestTimeout window, the test would fail.
                    if (lineFolds && readCount % 10 == 0)
                    {
                        buffer[0] = (byte)'\n';
                        buffer[1] = (byte)' ';
                        return 2;
                    }
                    else
                    {
                        buffer[0] = (byte)'a';
                        return 1;
                    }
                }

                responseComplete = true;

                Debug.Assert(buffer.Length >= 4);
                return Encoding.ASCII.GetBytes("\r\n\r\n", buffer);
            };

            var responseStream = new DelegateDelegatingStream(Stream.Null)
            {
                ReadAsyncMemoryFunc = (memory, _) => new ValueTask<int>(readFunc(memory.Span)),
                ReadFunc = (array, offset, length) => readFunc(array.AsSpan(offset, length)),
                ReadSpanFunc = buffer => readFunc(buffer),
                DisposeFunc = _ => streamDisposed = true
            };

            using var client = new HttpClient(new SocketsHttpHandler
            {
                ConnectCallback = (_, _) => new ValueTask<Stream>(responseStream),
                MaxResponseHeadersLength = 1024 * 1024 // 1 GB
            })
            {
                Timeout = TestHelper.PassingTestTimeout
            };

            var request = new HttpRequestMessage(HttpMethod.Get, "http://foo");

            using HttpResponseMessage response = async
                ? await client.SendAsync(request)
                : client.Send(request);

            response.EnsureSuccessStatusCode();

            HttpHeaders headers = trailingHeaders
                ? response.TrailingHeaders
                : response.Headers;
            Assert.True(headers.NonValidated.Contains("Long-Header"));
        }
    }

    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http2 : SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength
    {
        public SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/74896")]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http3 : SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength
    {
        public SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Socket is not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_ConnectionUpgrade_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_ConnectionUpgrade_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task UpgradeConnection_ReturnsReadableAndWritableStream()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    await server.AcceptConnectionAsync(async (LoopbackServer.Connection connection) =>
                    {
                        Task<List<string>> serverTask = connection.ReadRequestHeaderAndSendCustomResponseAsync($"HTTP/1.1 101 Switching Protocols\r\nDate: {DateTimeOffset.UtcNow:R}\r\n\r\n");

                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (Stream clientStream = await (await getResponseTask).Content.ReadAsStreamAsync(TestAsync))
                        {
                            // Boolean properties returning correct values
                            Assert.True(clientStream.CanWrite);
                            Assert.True(clientStream.CanRead);
                            Assert.False(clientStream.CanSeek);

                            // Not supported operations
                            Assert.Throws<NotSupportedException>(() => clientStream.Length);
                            Assert.Throws<NotSupportedException>(() => clientStream.Position);
                            Assert.Throws<NotSupportedException>(() => clientStream.Position = 0);
                            Assert.Throws<NotSupportedException>(() => clientStream.Seek(0, SeekOrigin.Begin));
                            Assert.Throws<NotSupportedException>(() => clientStream.SetLength(0));

                            // Invalid arguments
                            var nonWritableStream = new MemoryStream(new byte[1], false);
                            var disposedStream = new MemoryStream();
                            disposedStream.Dispose();
                            Assert.Throws<ArgumentNullException>(() => clientStream.CopyTo(null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.CopyTo(Stream.Null, 0));
                            Assert.Throws<ArgumentNullException>(() => { clientStream.CopyToAsync(null, 100, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.CopyToAsync(Stream.Null, 0, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.CopyToAsync(Stream.Null, -1, default); });
                            Assert.Throws<NotSupportedException>(() => { clientStream.CopyToAsync(nonWritableStream, 100, default); });
                            Assert.Throws<ObjectDisposedException>(() => { clientStream.CopyToAsync(disposedStream, 100, default); });
                            Assert.Throws<ArgumentNullException>(() => clientStream.Read(null, 0, 100));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.Read(new byte[1], -1, 1));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.Read(new byte[1], 2, 1));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.Read(new byte[1], 0, -1));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.Read(new byte[1], 0, 2));
                            Assert.Throws<ArgumentNullException>(() => clientStream.BeginRead(null, 0, 100, null, null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.BeginRead(new byte[1], -1, 1, null, null));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.BeginRead(new byte[1], 2, 1, null, null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.BeginRead(new byte[1], 0, -1, null, null));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.BeginRead(new byte[1], 0, 2, null, null));
                            Assert.Throws<ArgumentNullException>(() => clientStream.EndRead(null));
                            Assert.Throws<ArgumentNullException>(() => { clientStream.ReadAsync(null, 0, 100, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.ReadAsync(new byte[1], -1, 1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { clientStream.ReadAsync(new byte[1], 2, 1, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.ReadAsync(new byte[1], 0, -1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { clientStream.ReadAsync(new byte[1], 0, 2, default); });

                            // Validate writing APIs on clientStream

                            clientStream.WriteByte((byte)'!');
                            clientStream.Write("\r\n"u8.ToArray(), 0, 2);
                            Assert.Equal("!", await connection.ReadLineAsync());

                            clientStream.Write("hello\r\n"u8);
                            Assert.Equal("hello", await connection.ReadLineAsync());

                            await clientStream.WriteAsync("world\r\n"u8.ToArray(), 0, 7);
                            Assert.Equal("world", await connection.ReadLineAsync());

                            await clientStream.WriteAsync(new Memory<byte>("and\r\n"u8.ToArray(), 0, 5));
                            Assert.Equal("and", await connection.ReadLineAsync());

                            await Task.Factory.FromAsync(clientStream.BeginWrite, clientStream.EndWrite, "beyond\r\n"u8.ToArray(), 0, 8, null);
                            Assert.Equal("beyond", await connection.ReadLineAsync());

                            clientStream.Flush();
                            await clientStream.FlushAsync();

                            // Validate reading APIs on clientStream
                            await connection.Stream.WriteAsync("abcdefghijklmnopqrstuvwxyz"u8.ToArray());
                            var buffer = new byte[1];

                            Assert.Equal('a', clientStream.ReadByte());

                            Assert.Equal(1, clientStream.Read(buffer, 0, 1));
                            Assert.Equal((byte)'b', buffer[0]);

                            Assert.Equal(1, clientStream.Read(new Span<byte>(buffer, 0, 1)));
                            Assert.Equal((byte)'c', buffer[0]);

                            Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal((byte)'d', buffer[0]);

                            Assert.Equal(1, await clientStream.ReadAsync(new Memory<byte>(buffer, 0, 1)));
                            Assert.Equal((byte)'e', buffer[0]);

                            Assert.Equal(1, await Task.Factory.FromAsync(clientStream.BeginRead, clientStream.EndRead, buffer, 0, 1, null));
                            Assert.Equal((byte)'f', buffer[0]);

                            var ms = new MemoryStream();
                            Task copyTask = clientStream.CopyToAsync(ms);

                            string bigString = string.Concat(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", 1000));
                            Task lotsOfDataSent = connection.SendResponseAsync(Encoding.ASCII.GetBytes(bigString));
                            connection.Socket.Shutdown(SocketShutdown.Send);
                            await copyTask;
                            await lotsOfDataSent;
                            Assert.Equal("ghijklmnopqrstuvwxyz" + bigString, Encoding.ASCII.GetString(ms.ToArray()));
                        }
                    });
                }
            });
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_Connect_Test : HttpClientHandler_Connect_Test
    {
        public SocketsHttpHandler_Connect_Test(ITestOutputHelper output) : base(output) { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Socket is not supported on Browser")]
    public sealed class SocketsHttpHandler_HttpClientHandler_ConnectionPooling_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_HttpClientHandler_ConnectionPooling_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task MultipleIterativeRequests_SameConnectionReused()
        {
            using (HttpClient client = CreateHttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "\r\n";

                Task<string> firstRequest = client.GetStringAsync(uri);
                using (Socket server = await listener.AcceptAsync())
                using (var serverStream = new NetworkStream(server, ownsSocket: false))
                using (var serverReader = new StreamReader(serverStream))
                {
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync())) ;
                    await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await firstRequest;

                    Task<Socket> secondAccept = listener.AcceptAsync(); // shouldn't complete

                    Task<string> additionalRequest = client.GetStringAsync(uri);
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync())) ;
                    await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await additionalRequest;

                    Assert.False(secondAccept.IsCompleted, $"Second accept should never complete");
                }
            }
        }

        [OuterLoop("Incurs a delay")]
        [Fact]
        public async Task ServerDisconnectsAfterInitialRequest_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    // Make multiple requests iteratively.
                    for (int i = 0; i < 2; i++)
                    {
                        Task<string> request = client.GetStringAsync(uri);
                        await server.AcceptConnectionSendResponseAndCloseAsync();
                        await request;

                        if (i == 0)
                        {
                            await Task.Delay(2000); // give client time to see the closing before next connect
                        }
                    }
                });
            }
        }

        [Fact]
        public async Task ServerSendsGarbageAfterInitialRequest_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    var releaseServer = new TaskCompletionSource();

                    // Make multiple requests iteratively.

                    Task serverTask1 = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.WriteStringAsync(LoopbackServer.GetHttpResponse(connectionClose: false) + "here is a bunch of garbage");
                        await releaseServer.Task; // keep connection alive on the server side
                    });
                    await client.GetStringAsync(uri);

                    Task serverTask2 = server.AcceptConnectionSendCustomResponseAndCloseAsync(LoopbackServer.GetHttpResponse(connectionClose: true));
                    await new[] { client.GetStringAsync(uri), serverTask2 }.WhenAllOrAnyFailed();

                    releaseServer.SetResult();
                    await serverTask1;
                });
            }
        }

        [Fact]
        public async Task ServerSendsConnectionClose_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    string responseBody =
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        "Content-Length: 0\r\n" +
                        "Connection: close\r\n" +
                        "\r\n";

                    // Make first request.
                    Task<string> request1 = client.GetStringAsync(uri);
                    await server.AcceptConnectionAsync(async connection1 =>
                    {
                        await connection1.ReadRequestHeaderAndSendCustomResponseAsync(responseBody);
                        await request1;

                        // Make second request and expect it to be served from a different connection.
                        Task<string> request2 = client.GetStringAsync(uri);
                        await server.AcceptConnectionAsync(async connection2 =>
                        {
                            await connection2.ReadRequestHeaderAndSendCustomResponseAsync(responseBody);
                            await request2;
                        });
                    });
                });
            }
        }

        [Theory]
        [InlineData("PooledConnectionLifetime")]
        [InlineData("PooledConnectionIdleTimeout")]
        public async Task SmallConnectionTimeout_SubsequentRequestUsesDifferentConnection(string timeoutPropertyName)
        {
            using (var handler = new SocketsHttpHandler())
            {
                switch (timeoutPropertyName)
                {
                    case "PooledConnectionLifetime": handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    case "PooledConnectionIdleTimeout": handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    default: throw new ArgumentOutOfRangeException(nameof(timeoutPropertyName));
                }

                using (HttpClient client = CreateHttpClient(handler))
                {
                    await LoopbackServer.CreateServerAsync(async (server, uri) =>
                    {
                        // Make first request.
                        Task<string> request1 = client.GetStringAsync(uri);
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                            await request1;

                            // Wait a small amount of time before making the second request, to give the first request time to timeout.
                            await Task.Delay(100);

                            // Make second request and expect it to be served from a different connection.
                            Task<string> request2 = client.GetStringAsync(uri);
                            await server.AcceptConnectionAsync(async connection2 =>
                            {
                                await connection2.ReadRequestHeaderAndSendResponseAsync();
                                await request2;
                            });
                        });
                    });
                }
            }
        }

        [Theory]
        [InlineData("PooledConnectionLifetime")]
        [InlineData("PooledConnectionIdleTimeout")]
        public async Task Http2_SmallConnectionTimeout_SubsequentRequestUsesDifferentConnection(string timeoutPropertyName)
        {
            await Http2LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler(HttpVersion.Version20);
                SocketsHttpHandler s = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                switch (timeoutPropertyName)
                {
                    case "PooledConnectionLifetime": s.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    case "PooledConnectionIdleTimeout": s.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    default: throw new ArgumentOutOfRangeException(nameof(timeoutPropertyName));
                }

                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestVersion = HttpVersion.Version20;
                    Task<string> request1 = client.GetStringAsync(url);

                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync();
                    await connection.SendDefaultResponseAsync(streamId);
                    await request1;

                    // Wait a small amount of time before making the second request, to give the first request time to timeout.
                    await Task.Delay(100);
                    // Grab reference to underlying socket and stream to make sure they are not disposed and closed.
                    (SocketWrapper socket, Stream stream) = connection.ResetNetwork();

                    // Make second request and expect it to be served from a different connection.
                    Task<string> request2 = client.GetStringAsync(url);
                    connection = await server.EstablishConnectionAsync();
                    streamId = await connection.ReadRequestHeaderAsync();
                    await connection.SendDefaultResponseAsync(streamId);
                    await request2;

                    // Close underlying socket from first connection.
                    socket.Close();
                }
            });
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ConnectionsPooledThenDisposed_NoUnobservedTaskExceptions(bool secure)
        {
            RemoteExecutor.Invoke(async (secureString, useVersionString) =>
            {
                var releaseServer = new TaskCompletionSource();
                await LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    using (var handler = CreateSocketsHttpHandler(allowAllCertificates: true))
                    using (HttpClient client = CreateHttpClient(handler, useVersionString))
                    {
                        handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1);

                        var exceptions = new List<Exception>();
                        TaskScheduler.UnobservedTaskException += (s, e) => exceptions.Add(e.Exception);

                        await client.GetStringAsync(uri);
                        await Task.Delay(10); // any value >= the lifetime
                        Task ignored = client.GetStringAsync(uri); // force the pool to look for the previous connection and find it's too old
                        await Task.Delay(100); // give some time for the connection close to fail pending reads

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        // Note that there are race conditions here such that we may not catch every failure,
                        // and thus could have some false negatives, but there won't be any false positives.
                        Assert.True(exceptions.Count == 0, string.Concat(exceptions));

                        releaseServer.SetResult();
                    }
                }, server => server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: "hello world");
                    await releaseServer.Task;
                }),
                new LoopbackServer.Options { UseSsl = bool.Parse(secureString) });
            }, secure.ToString(), UseVersion.ToString()).Dispose();
        }

        [OuterLoop]
        [Fact]
        public void HandlerDroppedWithoutDisposal_NotKeptAlive()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            HandlerDroppedWithoutDisposal_NotKeptAliveCore(tcs);
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.True(tcs.Task.IsCompleted);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandlerDroppedWithoutDisposal_NotKeptAliveCore(TaskCompletionSource setOnFinalized)
        {
            // This relies on knowing that in order for the connection pool to operate, it needs
            // to maintain a reference to the supplied IWebProxy.  As such, we provide a proxy
            // that when finalized will set our event, so that we can determine the state associated
            // with a handler has gone away.
            IWebProxy p = new PassthroughProxyWithFinalizerCallback(() => setOnFinalized.TrySetResult());

            // Make a bunch of requests and drop the associated HttpClient instances after making them, without disposal.
            Task.WaitAll((from i in Enumerable.Range(0, 10)
                          select LoopbackServer.CreateClientAndServerAsync(
                              url => CreateHttpClient(new SocketsHttpHandler { Proxy = p }).GetStringAsync(url),
                              server => server.AcceptConnectionSendResponseAndCloseAsync())).ToArray());
        }

        private sealed class PassthroughProxyWithFinalizerCallback : IWebProxy
        {
            private readonly Action _callback;

            public PassthroughProxyWithFinalizerCallback(Action callback) => _callback = callback;
            ~PassthroughProxyWithFinalizerCallback() => _callback();

            public ICredentials Credentials { get; set; }
            public Uri GetProxy(Uri destination) => destination;
            public bool IsBypassed(Uri host) => true;
        }

        [Fact]
        public async Task ProxyAuth_SameConnection_Succeeds()
        {
            Task serverTask = LoopbackServer.CreateServerAsync(async (proxyServer, proxyUrl) =>
            {
                string responseBody =
                        "HTTP/1.1 407 Proxy Auth Required\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        "Proxy-Authenticate: Basic\r\n" +
                        "Content-Length: 0\r\n" +
                        "\r\n";

                using (var handler = new HttpClientHandler())
                {
                    handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, new NetworkCredential("abc", "password"));

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        Task<string> request = client.GetStringAsync($"http://notarealserver.com/");

                        await proxyServer.AcceptConnectionAsync(async connection =>
                        {
                            // Get first request, no body for GET.
                            await connection.ReadRequestHeaderAndSendCustomResponseAsync(responseBody).ConfigureAwait(false);
                            // Client should send another request after being rejected with 407.
                            await connection.ReadRequestHeaderAndSendResponseAsync(content: "OK").ConfigureAwait(false);
                        });

                        string response = await request;
                        Assert.Equal("OK", response);
                    }
                }
            });
            await serverTask.WaitAsync(TestHelper.PassingTestTimeout);
        }
    }

    // System.Net.Sockets is not supported on this platform
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandler_PublicAPIBehavior_Test
    {
        [Fact]
        public void AllowAutoRedirect_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.AllowAutoRedirect);

                handler.AllowAutoRedirect = true;
                Assert.True(handler.AllowAutoRedirect);

                handler.AllowAutoRedirect = false;
                Assert.False(handler.AllowAutoRedirect);
            }
        }

        [Fact]
        public void AutomaticDecompression_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.GZip;
                Assert.Equal(DecompressionMethods.GZip, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.Deflate;
                Assert.Equal(DecompressionMethods.Deflate, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                Assert.Equal(DecompressionMethods.GZip | DecompressionMethods.Deflate, handler.AutomaticDecompression);
            }
        }

        [Fact]
        public void CookieContainer_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                CookieContainer container = handler.CookieContainer;
                Assert.Same(container, handler.CookieContainer);

                var newContainer = new CookieContainer();
                handler.CookieContainer = newContainer;
                Assert.Same(newContainer, handler.CookieContainer);
            }
        }

        [Fact]
        public void Credentials_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.Credentials);

                var newCredentials = new NetworkCredential("username", "password");
                handler.Credentials = newCredentials;
                Assert.Same(newCredentials, handler.Credentials);
            }
        }

        [Fact]
        public void DefaultProxyCredentials_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.DefaultProxyCredentials);

                var newCredentials = new NetworkCredential("username", "password");
                handler.DefaultProxyCredentials = newCredentials;
                Assert.Same(newCredentials, handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public void KeepAlivePing_GetSet_Roundtrips()
        {
            using var handler = new SocketsHttpHandler();

            var testTimeSpanValue = TimeSpan.FromSeconds(5);
            var invalidTimeSpanValue = TimeSpan.FromTicks(TimeSpan.TicksPerSecond - 1);

            Assert.Equal(TimeSpan.FromSeconds(20), handler.KeepAlivePingTimeout);
            handler.KeepAlivePingTimeout = testTimeSpanValue;
            Assert.Equal(testTimeSpanValue, handler.KeepAlivePingTimeout);

            Assert.Equal(Timeout.InfiniteTimeSpan, handler.KeepAlivePingDelay);
            handler.KeepAlivePingDelay = testTimeSpanValue;
            Assert.Equal(testTimeSpanValue, handler.KeepAlivePingDelay);

            Assert.Equal(HttpKeepAlivePingPolicy.Always, handler.KeepAlivePingPolicy);
            handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
            Assert.Equal(HttpKeepAlivePingPolicy.WithActiveRequests, handler.KeepAlivePingPolicy);

            Assert.Throws<ArgumentOutOfRangeException>(() => handler.KeepAlivePingTimeout = invalidTimeSpanValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => handler.KeepAlivePingDelay = invalidTimeSpanValue);
        }

        [Fact]
        public void MaxAutomaticRedirections_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(50, handler.MaxAutomaticRedirections);

                handler.MaxAutomaticRedirections = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxAutomaticRedirections);

                handler.MaxAutomaticRedirections = 1;
                Assert.Equal(1, handler.MaxAutomaticRedirections);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxAutomaticRedirections = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxAutomaticRedirections = -1);
            }
        }

        [Fact]
        public void MaxConnectionsPerServer_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);

                handler.MaxConnectionsPerServer = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);

                handler.MaxConnectionsPerServer = 1;
                Assert.Equal(1, handler.MaxConnectionsPerServer);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxConnectionsPerServer = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxConnectionsPerServer = -1);
            }
        }

        [Fact]
        public void MaxResponseHeadersLength_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(64, handler.MaxResponseHeadersLength);

                handler.MaxResponseHeadersLength = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxResponseHeadersLength);

                handler.MaxResponseHeadersLength = 1;
                Assert.Equal(1, handler.MaxResponseHeadersLength);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = -1);
            }
        }

        [Fact]
        public void PreAuthenticate_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.False(handler.PreAuthenticate);

                handler.PreAuthenticate = false;
                Assert.False(handler.PreAuthenticate);

                handler.PreAuthenticate = true;
                Assert.True(handler.PreAuthenticate);
            }
        }

        [Fact]
        public void PooledConnectionIdleTimeout_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromMinutes(1), handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan;
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(0);
                Assert.Equal(TimeSpan.FromSeconds(0), handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1);
                Assert.Equal(TimeSpan.FromSeconds(1), handler.PooledConnectionIdleTimeout);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(-2));
            }
        }

        [Fact]
        public void PooledConnectionLifetime_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = Timeout.InfiniteTimeSpan;
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = TimeSpan.FromSeconds(0);
                Assert.Equal(TimeSpan.FromSeconds(0), handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = TimeSpan.FromSeconds(1);
                Assert.Equal(TimeSpan.FromSeconds(1), handler.PooledConnectionLifetime);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(-2));
            }
        }

        [Fact]
        public void Properties_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                IDictionary<string, object> props = handler.Properties;
                Assert.NotNull(props);
                Assert.Empty(props);

                props.Add("hello", "world");
                Assert.Equal(1, props.Count);
                Assert.Equal("world", props["hello"]);
            }
        }

        [Fact]
        public void Proxy_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.Proxy);

                var proxy = new WebProxy();
                handler.Proxy = proxy;
                Assert.Same(proxy, handler.Proxy);
            }
        }

        [Fact]
        public void SslOptions_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                SslClientAuthenticationOptions options = handler.SslOptions;
                Assert.NotNull(options);

                Assert.True(options.AllowRenegotiation);
                Assert.Null(options.ApplicationProtocols);
                Assert.Equal(X509RevocationMode.NoCheck, options.CertificateRevocationCheckMode);
                Assert.Null(options.ClientCertificates);
                Assert.Equal(SslProtocols.None, options.EnabledSslProtocols);
                Assert.Equal(EncryptionPolicy.RequireEncryption, options.EncryptionPolicy);
                Assert.Null(options.LocalCertificateSelectionCallback);
                Assert.Null(options.RemoteCertificateValidationCallback);
                Assert.Null(options.TargetHost);

                Assert.Same(options, handler.SslOptions);

                var newOptions = new SslClientAuthenticationOptions();
                handler.SslOptions = newOptions;
                Assert.Same(newOptions, handler.SslOptions);
            }
        }

        [Fact]
        public void UseCookies_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.UseCookies);

                handler.UseCookies = true;
                Assert.True(handler.UseCookies);

                handler.UseCookies = false;
                Assert.False(handler.UseCookies);
            }
        }

        [Fact]
        public void UseProxy_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.UseProxy);

                handler.UseProxy = false;
                Assert.False(handler.UseProxy);

                handler.UseProxy = true;
                Assert.True(handler.UseProxy);
            }
        }

        [Fact]
        public void ConnectCallback_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.ConnectCallback);

                Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> f = (context, token) => default;

                handler.ConnectCallback = f;
                Assert.Equal(f, handler.ConnectCallback);

                handler.ConnectCallback = null;
                Assert.Null(handler.ConnectCallback);
            }
        }

        [Fact]
        public void PlaintextStreamFilter_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.PlaintextStreamFilter);

                Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>> f = (context, token) => default;

                handler.PlaintextStreamFilter = f;
                Assert.Equal(f, handler.PlaintextStreamFilter);

                handler.PlaintextStreamFilter = null;
                Assert.Null(handler.PlaintextStreamFilter);
            }
        }

        [Fact]
        public void InitialHttp2StreamWindowSize_GetSet_Roundtrips()
        {
            using var handler = new SocketsHttpHandler();
            Assert.Equal(HttpClientHandlerTestBase.DefaultInitialWindowSize, handler.InitialHttp2StreamWindowSize); // default value

            handler.InitialHttp2StreamWindowSize = 1048576;
            Assert.Equal(1048576, handler.InitialHttp2StreamWindowSize);

            handler.InitialHttp2StreamWindowSize = HttpClientHandlerTestBase.DefaultInitialWindowSize;
            Assert.Equal(HttpClientHandlerTestBase.DefaultInitialWindowSize, handler.InitialHttp2StreamWindowSize);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(65534)]
        [InlineData(32 * 1024 * 1024)]
        public void InitialHttp2StreamWindowSize_InvalidValue_ThrowsArgumentOutOfRangeException(int value)
        {
            using var handler = new SocketsHttpHandler();
            Assert.Throws<ArgumentOutOfRangeException>(() => handler.InitialHttp2StreamWindowSize = value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AfterDisposeSendAsync_GettersUsable_SettersThrow(bool dispose)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Type expectedExceptionType;
                if (dispose)
                {
                    handler.Dispose();
                    expectedExceptionType = typeof(ObjectDisposedException);
                }
                else
                {
                    using (var c = new HttpMessageInvoker(handler, disposeHandler: false))
                        await Assert.ThrowsAnyAsync<Exception>(() =>
                            c.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("/shouldquicklyfail", UriKind.Relative)), default));
                    expectedExceptionType = typeof(InvalidOperationException);
                }

                Assert.True(handler.AllowAutoRedirect);
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
                Assert.NotNull(handler.CookieContainer);
                Assert.Null(handler.Credentials);
                Assert.Null(handler.DefaultProxyCredentials);
                Assert.Equal(50, handler.MaxAutomaticRedirections);
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
                Assert.Equal(64, handler.MaxResponseHeadersLength);
                Assert.False(handler.PreAuthenticate);
                Assert.Equal(TimeSpan.FromMinutes(1), handler.PooledConnectionIdleTimeout);
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);
                Assert.NotNull(handler.Properties);
                Assert.Null(handler.Proxy);
                Assert.NotNull(handler.SslOptions);
                Assert.True(handler.UseCookies);
                Assert.True(handler.UseProxy);
                Assert.Null(handler.ConnectCallback);
                Assert.Null(handler.PlaintextStreamFilter);
                Assert.Equal(HttpClientHandlerTestBase.DefaultInitialWindowSize, handler.InitialHttp2StreamWindowSize);

                Assert.Throws(expectedExceptionType, () => handler.AllowAutoRedirect = false);
                Assert.Throws(expectedExceptionType, () => handler.AutomaticDecompression = DecompressionMethods.GZip);
                Assert.Throws(expectedExceptionType, () => handler.CookieContainer = new CookieContainer());
                Assert.Throws(expectedExceptionType, () => handler.Credentials = new NetworkCredential("anotheruser", "anotherpassword"));
                Assert.Throws(expectedExceptionType, () => handler.DefaultProxyCredentials = new NetworkCredential("anotheruser", "anotherpassword"));
                Assert.Throws(expectedExceptionType, () => handler.MaxAutomaticRedirections = 2);
                Assert.Throws(expectedExceptionType, () => handler.MaxConnectionsPerServer = 2);
                Assert.Throws(expectedExceptionType, () => handler.MaxResponseHeadersLength = 2);
                Assert.Throws(expectedExceptionType, () => handler.PreAuthenticate = false);
                Assert.Throws(expectedExceptionType, () => handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(2));
                Assert.Throws(expectedExceptionType, () => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(2));
                Assert.Throws(expectedExceptionType, () => handler.Proxy = new WebProxy());
                Assert.Throws(expectedExceptionType, () => handler.SslOptions = new SslClientAuthenticationOptions());
                Assert.Throws(expectedExceptionType, () => handler.UseCookies = false);
                Assert.Throws(expectedExceptionType, () => handler.UseProxy = false);
                Assert.Throws(expectedExceptionType, () => handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(5));
                Assert.Throws(expectedExceptionType, () => handler.KeepAlivePingDelay = TimeSpan.FromSeconds(5));
                Assert.Throws(expectedExceptionType, () => handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests);
                Assert.Throws(expectedExceptionType, () => handler.ConnectCallback = (context, token) => default);
                Assert.Throws(expectedExceptionType, () => handler.PlaintextStreamFilter = (context, token) => default);
                Assert.Throws(expectedExceptionType, () => handler.InitialHttp2StreamWindowSize = 128 * 1024);
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Headers.Location are not supported on Browser")]
    public sealed class SocketsHttpHandlerTest_LocationHeader
    {
        private static readonly byte[] s_redirectResponseBefore = Encoding.ASCII.GetBytes(
            "HTTP/1.1 301 Moved Permanently\r\n" +
            "Connection: close\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Location: ");

        private static readonly byte[] s_redirectResponseAfter = Encoding.ASCII.GetBytes(
            "\r\n" +
            "Server: Loopback\r\n" +
            "\r\n" +
            "0\r\n\r\n");

        [Theory]
        // US-ASCII only
        [InlineData("http://a/", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/' })]
        [InlineData("http://a/asdasd", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', (byte)'a', (byte)'s', (byte)'d', (byte)'a', (byte)'s', (byte)'d' })]
        // 2, 3, 4 byte UTF-8 characters
        [InlineData("http://a/\u00A2", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC2, 0xA2 })]
        [InlineData("http://a/\u20AC", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xE2, 0x82, 0xAC })]
        [InlineData("http://a/\uD800\uDF48", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xF0, 0x90, 0x8D, 0x88 })]
        // 3 Polish letters
        [InlineData("http://a/\u0105\u015B\u0107", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC4, 0x85, 0xC5, 0x9B, 0xC4, 0x87 })]
        // Negative cases - should be interpreted as ISO-8859-1
        // Invalid utf-8 sequence (continuation without start)
        [InlineData("http://a/%C2%80", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0b10000000 })]
        // Invalid utf-8 sequence (not allowed character)
        [InlineData("http://a/\u00C3\u0028", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC3, 0x28 })]
        // Incomplete utf-8 sequence
        [InlineData("http://a/\u00C2", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC2 })]
        public async Task LocationHeader_DecodesUtf8_Success(string expected, byte[] location)
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.AllowAutoRedirect = false;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        Assert.Equal(expected, response.Headers.Location.ToString());
                    }
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(PreperateResponseWithRedirect(location)));
        }

        private static byte[] PreperateResponseWithRedirect(byte[] location)
        {
            return s_redirectResponseBefore.Concat(location).Concat(s_redirectResponseAfter).ToArray();
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandlerTest_Http2 : HttpClientHandlerTest_Http2
    {
        public SocketsHttpHandlerTest_Http2(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_ConnectionLimitNotReached_ConcurrentRequestsSuccessfullyHandled()
        {
            const int MaxConcurrentStreams = 2;

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler();
            using HttpClient client = CreateHttpClient(handler);
            server.AllowMultipleConnections = true;

            List<Task<HttpResponseMessage>> sendTasks = new();
            List<Http2LoopbackConnection> connections = new();
            List<int> acceptedStreams = new();

            for (int i = 0; i < 3; i++)
            {
                Http2LoopbackConnection connection = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                connections.Add(connection);

                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);

                acceptedStreams.AddRange(await AcceptRequests(connection, MaxConcurrentStreams).ConfigureAwait(false));
            }

            Assert.Equal(3 * MaxConcurrentStreams, acceptedStreams.Count);
            Assert.Equal(sendTasks.Count, acceptedStreams.Count);

            int responseIndex = 0;
            List<Task> responseTasks = new();
            foreach (Http2LoopbackConnection connection in connections)
            {
                for (int i = 0; i < MaxConcurrentStreams; i++)
                {
                    int streamId = acceptedStreams[responseIndex++];
                    responseTasks.Add(connection.SendDefaultResponseAsync(streamId));
                }
            }

            await TestHelper.WhenAllCompletedOrAnyFailed(responseTasks.ToArray()).ConfigureAwait(false);

            await VerifySendTasks(sendTasks).ConfigureAwait(false);
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_ManyRequestsEnqueuedSimultaneously_SufficientConnectionsCreated()
        {
            // This is equal to Http2Connection.InitialMaxConcurrentStreams, which is the limit we impose before we have received the peer's initial SETTINGS frame.
            // Setting it to this value avoids any complexity that would occur from having to retry requests if the actual limit from the peer is lower.
            const int MaxConcurrentStreams = 100;

            const int ConnectionCount = 3;

            // Just enough to force the third connection to be created.
            const int RequestCount = (ConnectionCount - 1) * MaxConcurrentStreams + 1;

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            server.AllowMultipleConnections = true;

            using SocketsHttpHandler handler = CreateHandler();
            using HttpClient client = CreateHttpClient(handler);

            List<Task<HttpResponseMessage>> sendTasks = new();

            AcquireAllStreamSlots(server, client, sendTasks, RequestCount);

            await using Http2LoopbackConnection c1 = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 100 });
            int[] streamIds1 = await AcceptRequests(c1, MaxConcurrentStreams);

            await using Http2LoopbackConnection c2 = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 100 });
            int[] streamIds2 = await AcceptRequests(c2, MaxConcurrentStreams);

            await using Http2LoopbackConnection c3 = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 100 });
            (int finalStreamId, _) = await c3.ReadAndParseRequestHeaderAsync();

            await SendResponses(c1, streamIds1);
            await SendResponses(c2, streamIds2);
            await c3.SendDefaultResponseAsync(finalStreamId);

            await VerifySendTasks(sendTasks);
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_InfiniteRequestsCompletelyBlockOneConnection_RemainingRequestsAreHandledByNewConnection()
        {
            const int MaxConcurrentStreams = 2;

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler();
            using HttpClient client = CreateHttpClient(handler);
            server.AllowMultipleConnections = true;

            List<Task<HttpResponseMessage>> sendTasks = new();

            Http2LoopbackConnection connection0 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
            AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);

            // Accept requests but don't send responses on connection 0
            int[] blockedStreamIds = await AcceptRequests(connection0, MaxConcurrentStreams).ConfigureAwait(false);

            Http2LoopbackConnection connection1 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
            AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);

            // Send responses on connection 1
            await SendResponses(connection1, await AcceptRequests(connection1, MaxConcurrentStreams).ConfigureAwait(false));

            // Send responses on connection 0
            await SendResponses(connection0, blockedStreamIds);

            await VerifySendTasks(sendTasks).ConfigureAwait(false);
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_OpenAndCloseMultipleConnections_Success()
        {
            if (PlatformDetection.IsAndroid && (PlatformDetection.IsX86Process || PlatformDetection.IsX64Process))
            {
                throw new SkipTestException("Currently this test is failing on Android API 29 (used on Android-x64 and Android-x86 emulators)");
            }

            const int MaxConcurrentStreams = 2;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            server.AllowMultipleConnections = true;

            // Allow 5 connections through the ConnectCallback.
            SemaphoreSlim connectCallbackSemaphore = new(initialCount: 5);

            using SocketsHttpHandler handler = CreateHandler();

            handler.ConnectCallback = async (context, ct) =>
            {
                await connectCallbackSemaphore.WaitAsync(ct);

                return await DefaultConnectCallback(context.DnsEndPoint, ct);
            };

            using (HttpClient client = CreateHttpClient(handler))
            {
                List<Task<HttpResponseMessage>> sendTasks = new();

                Http2LoopbackConnection connection0 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                int[] streamIds0 = await AcceptRequests(connection0, MaxConcurrentStreams).ConfigureAwait(false);

                Http2LoopbackConnection connection1 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                int[] streamIds1 = await AcceptRequests(connection1, MaxConcurrentStreams).ConfigureAwait(false);

                Http2LoopbackConnection connection2 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                int[] streamIds2 = await AcceptRequests(connection2, MaxConcurrentStreams).ConfigureAwait(false);

                await TestHelper.WhenAllCompletedOrAnyFailed(
                    SendResponses(connection0, streamIds0),
                    SendResponses(connection1, streamIds1),
                    SendResponses(connection2, streamIds2))
                    .ConfigureAwait(false);

                await connection0.ShutdownIgnoringErrorsAsync(streamIds0[^1]).ConfigureAwait(false);
                await connection2.ShutdownIgnoringErrorsAsync(streamIds2[^1]).ConfigureAwait(false);

                // Fill all connection1's stream slots
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                streamIds1 = await AcceptRequests(connection1, MaxConcurrentStreams).ConfigureAwait(false);

                Http2LoopbackConnection connection3 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                int[] streamIds3 = await AcceptRequests(connection3, MaxConcurrentStreams).ConfigureAwait(false);

                Http2LoopbackConnection connection4 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks, MaxConcurrentStreams);
                int[] streamIds4 = await AcceptRequests(connection4, MaxConcurrentStreams).ConfigureAwait(false);

                await TestHelper.WhenAllCompletedOrAnyFailed(
                   SendResponses(connection1, streamIds1),
                   SendResponses(connection3, streamIds3),
                   SendResponses(connection4, streamIds4))
                   .ConfigureAwait(false);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [OuterLoop("Incurs long delay")]
        public async Task Http2_MultipleConnectionsEnabled_IdleConnectionTimeoutExpired_ConnectionRemovedAndNewCreated()
        {
            const int MaxConcurrentStreams = 2;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            server.AllowMultipleConnections = true;

            SemaphoreSlim connectCallbackSemaphore = new(initialCount: 2);

            using SocketsHttpHandler handler = CreateHandler();
            handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(20);

            handler.ConnectCallback = async (context, ct) =>
            {
                await connectCallbackSemaphore.WaitAsync(ct);

                return await DefaultConnectCallback(context.DnsEndPoint, ct);
            };

            using (HttpClient client = CreateHttpClient(handler))
            {
                List<Task<HttpResponseMessage>> sendTasks0 = new();
                List<Task<HttpResponseMessage>> sendTasks1 = new();
                List<Task<HttpResponseMessage>> sendTasks2 = new();

                Http2LoopbackConnection connection0 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks0, MaxConcurrentStreams);
                int[] streamIds0 = await AcceptRequests(connection0, MaxConcurrentStreams).ConfigureAwait(false);

                Http2LoopbackConnection connection1 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks1, MaxConcurrentStreams);
                await SendResponses(connection1, await AcceptRequests(connection1, MaxConcurrentStreams).ConfigureAwait(false));

                // Complete all the requests on connection1.
                await VerifySendTasks(sendTasks1).ConfigureAwait(false);

                // Wait until the idle connection timeout expires.
                await connection1.WaitForClientDisconnectAsync(false).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                Assert.True(connection1.IsInvalid);
                Assert.False(connection0.IsInvalid);

                // Due to a race condition in how a new Http2 connection is returned to the pool, we may have started a third connection attempt in the background.
                // We were blocking such attempts from going through to the Socket layer until now to avoid having to deal with the extra connect when accepting connection2 below.
                // Allow the third connection through the ConnectCallback now.
                connectCallbackSemaphore.Release();

                Http2LoopbackConnection connection2 = await PrepareConnection(server, client, MaxConcurrentStreams).ConfigureAwait(false);
                AcquireAllStreamSlots(server, client, sendTasks2, MaxConcurrentStreams);
                await SendResponses(connection2, await AcceptRequests(connection2, MaxConcurrentStreams).ConfigureAwait(false));

                // Make sure connection0 is still alive.
                await SendResponses(connection0, streamIds0).ConfigureAwait(false);

                await VerifySendTasks(sendTasks0).ConfigureAwait(false);
                await VerifySendTasks(sendTasks2).ConfigureAwait(false);
            }
        }

        private async Task VerifySendTasks(IReadOnlyList<Task<HttpResponseMessage>> sendTasks)
        {
            await TestHelper.WhenAllCompletedOrAnyFailed(sendTasks.ToArray()).ConfigureAwait(false);

            foreach (Task<HttpResponseMessage> sendTask in sendTasks)
            {
                using HttpResponseMessage response = await sendTask.ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static SocketsHttpHandler CreateHandler() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            PooledConnectionIdleTimeout = TimeSpan.FromHours(1),
            PooledConnectionLifetime = TimeSpan.FromHours(1),
            SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
        };

        private async Task<Http2LoopbackConnection> PrepareConnection(Http2LoopbackServer server, HttpClient client, uint maxConcurrentStreams)
        {
            Assert.True(maxConcurrentStreams > 0);

            Task<HttpResponseMessage> warmUpTask = client.GetAsync(server.Address);

            var concurrentStreamsSetting = new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = maxConcurrentStreams };

            Http2LoopbackConnection connection = await server.EstablishConnectionAsync(timeout: null, ackTimeout: TimeSpan.FromSeconds(10), concurrentStreamsSetting)
                .WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

            (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync().WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
            await connection.SendDefaultResponseAsync(streamId).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

            using HttpResponseMessage response = await warmUpTask.WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
            Assert.True(response.IsSuccessStatusCode);

            // SocketsHttpHandler.Http2Connection will always ACK the initial settings frame before responding to requests.
            return connection;
        }

        private static void AcquireAllStreamSlots(Http2LoopbackServer server, HttpClient client, List<Task<HttpResponseMessage>> sendTasks, uint maxConcurrentStreams)
        {
            for (int i = 0; i < maxConcurrentStreams; i++)
            {
                sendTasks.Add(client.GetAsync(server.Address));
            }
        }

        private async Task<int[]> AcceptRequests(Http2LoopbackConnection connection, int requestCount)
        {
            int[] streamIds = new int[requestCount];

            for (int i = 0; i < streamIds.Length; i++)
            {
                (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync().WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                streamIds[i] = streamId;
            }

            return streamIds;
        }

        private async Task SendResponses(Http2LoopbackConnection connection, IEnumerable<int> streamIds)
        {
            foreach (int streamId in streamIds)
            {
                await connection.SendDefaultResponseAsync(streamId).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
            }
        }
    }

    public abstract class SocketsHttpHandlerTest_ConnectCallback : HttpClientHandlerTestBase
    {
        public SocketsHttpHandlerTest_ConnectCallback(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ConnectCallback_ContextHasCorrectProperties_Success(bool syncRequest, bool syncCallback)
        {
            if (syncRequest && UseVersion > HttpVersion.Version11)
            {
                // Sync requests are only supported on 1.x
                return;
            }

            if (syncRequest && PlatformDetection.IsMobile)
            {
                // Sync requests are not supported on mobile platforms
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = UseVersion;
                    requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        Assert.Equal(uri.Host, context.DnsEndPoint.Host);
                        Assert.Equal(uri.Port, context.DnsEndPoint.Port);
                        Assert.Equal(requestMessage, context.InitialRequestMessage);

                        var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        if (syncCallback)
                        {
                            s.Connect(context.DnsEndPoint);
                        }
                        else
                        {
                            await s.ConnectAsync(context.DnsEndPoint, token);
                            await Task.Delay(1); // to increase the chances of the whole operation completing asynchronously, without consuming too much additional time
                        }
                        return new NetworkStream(s, ownsSocket: true);
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpResponseMessage response = await (syncRequest ?
                        Task.Run(() => client.Send(requestMessage)) :
                        client.SendAsync(requestMessage));
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_BindLocalAddress_Success(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                        await s.ConnectAsync(context.DnsEndPoint, token);
                        s.NoDelay = true;
                        return new NetworkStream(s, ownsSocket: true);
                    };

                    using HttpClient client = CreateHttpClient(handler);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    string response = await client.GetStringAsync(uri);
                    Assert.Equal("foo", response);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                }, options: options);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_UseMemoryBuffer_Success(bool useSsl)
        {
            (Stream clientStream, Stream serverStream) = ConnectedStreams.CreateBidirectional();

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };

            Task serverTask = Task.Run(async () =>
            {
                await using GenericLoopbackConnection loopbackConnection = await LoopbackServerFactory.CreateConnectionAsync(socket: null, serverStream, options);
                await loopbackConnection.InitializeConnectionAsync();

                HttpRequestData requestData = await loopbackConnection.ReadRequestDataAsync();
                await loopbackConnection.SendResponseAsync(content: "foo");

                Assert.Equal("/foo", requestData.Path);
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                socketsHandler.ConnectCallback = (context, token) => new ValueTask<Stream>(clientStream);

                using HttpClient client = CreateHttpClient(handler);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                string response = await client.GetStringAsync($"{(options.UseSsl ? "https" : "http")}://nowhere.invalid/foo");
                Assert.Equal("foo", response);
            });

            await new[] { serverTask, clientTask }.WhenAllOrAnyFailed(60_000);
        }

        [ConditionalTheory(nameof(PlatformSupportsUnixDomainSockets))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/44183", TestPlatforms.Windows)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_UseUnixDomainSocket_Success(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };

            string guid = $"{Guid.NewGuid():N}";
            UnixDomainSocketEndPoint serverEP = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), guid));
            Socket listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listenSocket.Bind(serverEP);
            listenSocket.Listen();

            using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = async (context, token) =>
            {
                string hostname = context.DnsEndPoint.Host;
                UnixDomainSocketEndPoint clientEP = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), hostname));

                Socket clientSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await clientSocket.ConnectAsync(clientEP);

                return new NetworkStream(clientSocket, ownsSocket: true);
            };

            using (HttpClient client = CreateHttpClient(handler))
            {
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                Task<string> clientTask = client.GetStringAsync($"{(options.UseSsl ? "https" : "http")}://{guid}/foo");

                Socket serverSocket = await listenSocket.AcceptAsync();
                await using (GenericLoopbackConnection loopbackConnection = await LoopbackServerFactory.CreateConnectionAsync(socket: null, new NetworkStream(serverSocket, ownsSocket: true), options))
                {
                    await loopbackConnection.InitializeConnectionAsync();

                    HttpRequestData requestData = await loopbackConnection.ReadRequestDataAsync();
                    Assert.Equal("/foo", requestData.Path);

                    await loopbackConnection.SendResponseAsync(content: "foo");

                    string response = await clientTask;
                    Assert.Equal("foo", response);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_ConnectionPrefix_Success(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };

            byte[] RequestPrefix = "request prefix\r\n"u8.ToArray();
            byte[] ResponsePrefix = "response prefix\r\n"u8.ToArray();

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listenSocket.Listen();

            using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: useSsl);
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = async (context, token) =>
            {
                Stream clientStream = await DefaultConnectCallback(listenSocket.LocalEndPoint, token);

                await clientStream.WriteAsync(RequestPrefix);

                byte[] buffer = new byte[ResponsePrefix.Length];
                await clientStream.ReadAsync(buffer);
                Assert.True(buffer.SequenceEqual(ResponsePrefix));

                return clientStream;
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            Task<string> clientTask = client.GetStringAsync($"{(options.UseSsl ? "https" : "http")}://nowhere.invalid/foo");

            Socket serverSocket = await listenSocket.AcceptAsync();
            Stream serverStream = new NetworkStream(serverSocket, ownsSocket: true);

            byte[] buffer = new byte[RequestPrefix.Length];
            await serverStream.ReadAsync(buffer);
            Assert.True(buffer.SequenceEqual(RequestPrefix));

            await serverStream.WriteAsync(ResponsePrefix);

            await using GenericLoopbackConnection loopbackConnection = await LoopbackServerFactory.CreateConnectionAsync(socket: null, serverStream, options);
            await loopbackConnection.InitializeConnectionAsync();

            HttpRequestData requestData = await loopbackConnection.ReadRequestDataAsync();
            Assert.Equal("/foo", requestData.Path);

            await loopbackConnection.SendResponseAsync(content: "foo");

            string response = await clientTask;
            Assert.Equal("foo", response);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_StreamThrowsOnWrite_ExceptionAndStreamDisposed(bool useSsl)
        {
            const string ExceptionMessage = "THROWONWRITE";

            bool disposeCalled = false;

            using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: useSsl);
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = (context, token) =>
            {
                var throwOnWriteStream = new DelegateDelegatingStream(Stream.Null);
                throwOnWriteStream.WriteAsyncMemoryFunc = (buffer, token) => ValueTask.FromException(new IOException(ExceptionMessage));
                throwOnWriteStream.DisposeFunc = (_) => { disposeCalled = true; };
                throwOnWriteStream.DisposeAsyncFunc = () => { disposeCalled = true; return default; };
                return ValueTask.FromResult<Stream>(throwOnWriteStream);
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            HttpRequestException hre = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.GetStringAsync($"{(useSsl ? "https" : "http")}://nowhere.invalid/foo"));

            Debug.Assert(disposeCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_ExceptionDuringCallback_ThrowsHttpRequestExceptionWithInnerException(bool useSsl)
        {
            Exception e = new Exception("hello!");

            using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: useSsl);
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = (context, token) =>
            {
                throw e;
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            HttpRequestException hre = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.GetAsync($"{(useSsl ? "https" : "http")}://nowhere.invalid/foo"));
            Assert.Equal(e, hre.InnerException);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_ReturnsNull_ThrowsHttpRequestException(bool useSsl)
        {
            using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: useSsl);
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = (context, token) =>
            {
                return ValueTask.FromResult<Stream>(null);
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            HttpRequestException hre = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.GetAsync($"{(useSsl ? "https" : "http")}://nowhere.invalid/foo"));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_SslStream_OK(bool useSslStream)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    string[] parts = uri.Authority.Split(':', 2);
                    HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        TcpClient client = new TcpClient();
                        await client.ConnectAsync(parts[0], Int32.Parse(parts[1]));
                        if (useSslStream)
                        {
                            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions();
                            options.RemoteCertificateValidationCallback = (a, b, c, d) => true;
                            options.TargetHost = parts[0];
                            if (context.InitialRequestMessage.Version.Major == 2 && PlatformDetection.SupportsAlpn)
                            {
                                options.ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 };
                            }
                            var sslStream = new SslStream(client.GetStream());
                            await sslStream.AuthenticateAsClientAsync(options);
                            return sslStream;
                        }
                        else
                        {
                            return client.GetStream();
                        }
                    };
                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion);
                        HttpResponseMessage response = await client.SendAsync(request);
                        if (PlatformDetection.SupportsAlpn)
                        {
                            Assert.Equal(request.Version, response.Version);
                        }
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                }, options: new GenericLoopbackOptions { UseSsl = true });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public async Task ConnectCallback_DerivedSslStream_OK()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    string[] parts = uri.Authority.Split(':', 2);
                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => false;
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        TcpClient client = new TcpClient();
                        await client.ConnectAsync(parts[0], Int32.Parse(parts[1]));

                        SslClientAuthenticationOptions options = new SslClientAuthenticationOptions();
                        options.RemoteCertificateValidationCallback = (a, b, c, d) => true;
                        options.TargetHost = parts[0];
                        if (context.InitialRequestMessage.Version.Major == 2 && PlatformDetection.SupportsAlpn)
                        {
                            options.ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 };
                        }

                        MySsl myStream = new MySsl(client.GetStream());
                        await myStream.AuthenticateAsClientAsync(options);
                        return myStream;

                    };
                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion);
                        HttpResponseMessage response = await client.SendAsync(request);
                        if (PlatformDetection.SupportsAlpn)
                        {
                            Assert.Equal(request.Version, response.Version);
                        }
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                }, options: new GenericLoopbackOptions { UseSsl = true });
        }

        [Fact]
        public async Task ConnectCallback_NoAlpn_OK()
        {
            // Create HTTP 1.1 loopback. Http2 should downgrade
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    string[] parts = uri.Authority.Split(':', 2);

                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => false;
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        TcpClient client = new TcpClient();
                        await client.ConnectAsync(parts[0], Int32.Parse(parts[1]));

                        SslClientAuthenticationOptions options = new SslClientAuthenticationOptions();
                        options.RemoteCertificateValidationCallback = (a, b, c, d) => true;
                        options.TargetHost = parts[0];

                        SslStream myStream = new SslStream(client.GetStream());
                        await myStream.AuthenticateAsClientAsync(options);
                        return myStream;

                    };
                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion);
                        HttpResponseMessage response = await client.SendAsync(request);
                        Assert.Equal(1, response.Version.Major);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                }, options: new LoopbackServer.Options { UseSsl = true });
        }

        [Fact]
        public async Task ConnectCallback_MultipleRequests_EachRequestIsUsedOnce()
        {
            using HttpClientHandler handler = CreateHttpClientHandler();
            using HttpClient client = CreateHttpClient(handler);

            handler.MaxConnectionsPerServer = 2;

            TaskCompletionSource connectCallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource connectCallback1Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource connectCallback2Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

            var uri = new Uri("https://example.com");
            HttpRequestMessage request1 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request2 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request3 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

            List<int> requestsSeen = new();

            GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = async (context, cancellation) =>
            {
                if (context.InitialRequestMessage == request1) requestsSeen.Add(1);
                else if (context.InitialRequestMessage == request2) requestsSeen.Add(2);
                else if (context.InitialRequestMessage == request3) requestsSeen.Add(3);
                else requestsSeen.Add(-1);

                connectCallbackEntered.SetResult();

                if (context.InitialRequestMessage == request1) await connectCallback1Gate.Task.WaitAsync(TestHelper.PassingTestTimeout);
                if (context.InitialRequestMessage == request2) await connectCallback2Gate.Task.WaitAsync(TestHelper.PassingTestTimeout);

                throw new Exception("No connection");
            };

            Task requestTask1 = client.SendAsync(request1);
            await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
            Assert.Equal(new[] { 1 }, requestsSeen);

            connectCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task requestTask2, requestTask3;

            if (UseVersion.Major == 1)
            {
                requestTask2 = client.SendAsync(request2);
                await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
                Assert.Equal(new[] { 1, 2 }, requestsSeen);

                connectCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                requestTask3 = client.SendAsync(request3);
                await Task.Delay(1);
                Assert.Equal(new[] { 1, 2 }, requestsSeen);

                connectCallback2Gate.SetResult();
                await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
                Assert.Equal(new[] { 1, 2, 3 }, requestsSeen);

                // First request was not canceled when the second connection attempt failed
                Assert.NotEqual(TaskStatus.Faulted, requestTask1.Status);

                connectCallback1Gate.SetResult();
            }
            else if (UseVersion.Major == 2)
            {
                requestTask2 = client.SendAsync(request2);
                await Task.Delay(1);
                Assert.Equal(new[] { 1 }, requestsSeen);

                requestTask3 = client.SendAsync(request3);
                await Task.Delay(1);
                Assert.Equal(new[] { 1 }, requestsSeen);

                connectCallback1Gate.SetResult();
                await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
                Assert.Equal(new[] { 1, 2 }, requestsSeen);

                connectCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                connectCallback2Gate.SetResult();
                await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
                Assert.Equal(new[] { 1, 2, 3 }, requestsSeen);
            }
            else
            {
                throw new UnreachableException(UseVersion.ToString());
            }

            await Assert.ThrowsAsync<HttpRequestException>(() => requestTask1).WaitAsync(TestHelper.PassingTestTimeout);
            await Assert.ThrowsAsync<HttpRequestException>(() => requestTask2).WaitAsync(TestHelper.PassingTestTimeout);
            await Assert.ThrowsAsync<HttpRequestException>(() => requestTask3).WaitAsync(TestHelper.PassingTestTimeout);

            Assert.Equal(new[] { 1, 2, 3 }, requestsSeen);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectCallback_UseNamedPipe_Success(bool useSsl)
        {
            string guid = $"{Guid.NewGuid():N}";

            Task clientTask = Task.Run(async () =>
            {
                await using NamedPipeClientStream clientStream = new(".", pipeName: guid, PipeDirection.InOut, PipeOptions.WriteThrough | PipeOptions.Asynchronous, TokenImpersonationLevel.Anonymous);
                await clientStream.ConnectAsync(TestHelper.PassingTestTimeoutMilliseconds);

                using HttpClientHandler handler = CreateHttpClientHandler(UseVersion);
                using HttpClient client = CreateHttpClient(handler);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = (_, _) => new ValueTask<Stream>(clientStream);

                string url = $"{(useSsl ? "https" : "http")}://{guid}/foo";
                Assert.Equal("foo", await client.GetStringAsync(url).WaitAsync(TestHelper.PassingTestTimeoutMilliseconds));
            });

            Task serverTask = Task.Run(async () =>
            {
                await using NamedPipeServerStream serverStream = new(pipeName: guid, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
                await serverStream.WaitForConnectionAsync().WaitAsync(TestHelper.PassingTestTimeoutMilliseconds);

                // HTTP/1.1 doesn't work over named pipes when going through SslStream as it runs into a deadlock while performing the handshake.
                // The client is trying to write the request headers while the server is trying to write the end of its handshake.
                // As neither side is reading, the writes never complete.
                // We workaround that in tests by always having a pending read on the connection.
                await using Stream serverStreamWrapper = useSsl && UseVersion.Major == 1
                    ? new ReadAheadStream(serverStream, _output)
                    : serverStream;

                var options = new GenericLoopbackOptions { UseSsl = useSsl };
                await using GenericLoopbackConnection connection = await LoopbackServerFactory.CreateConnectionAsync(socket: null, serverStreamWrapper, options);
                await connection.InitializeConnectionAsync();

                HttpRequestData requestData = await connection.HandleRequestAsync(content: "foo").WaitAsync(TestHelper.PassingTestTimeoutMilliseconds);
                Assert.Equal("/foo", requestData.Path);
            });

            await TestHelper.WhenAllCompletedOrAnyFailedWithTimeout(GenericLoopbackServer.LoopbackServerTimeoutMilliseconds, clientTask, serverTask);
        }

        private static bool PlatformSupportsUnixDomainSockets => Socket.OSSupportsUnixDomainSockets;

        private sealed class ReadAheadStream : DelegatingStream
        {
            private readonly ITestOutputHelper _output;
            private readonly IO.Pipelines.Pipe _pipe;
            private readonly Stream _pipeReaderStream;

            public ReadAheadStream(Stream innerStream, ITestOutputHelper output) : base(innerStream)
            {
                _output = output;

                _pipe = new IO.Pipelines.Pipe();
                _pipeReaderStream = _pipe.Reader.AsStream();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await IO.Pipelines.StreamPipeExtensions.CopyToAsync(innerStream, _pipe.Writer);
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"ReadAheadStream ignored exception: {ex}");
                    }
                });
            }

            public override bool CanSeek => false;

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _pipeReaderStream.ReadAsync(buffer, offset, count, cancellationToken);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                _pipeReaderStream.ReadAsync(buffer, cancellationToken);

            public override int Read(byte[] buffer, int offset, int count) =>
                _pipeReaderStream.Read(buffer, offset, count);

            public override int Read(Span<byte> buffer) =>
                _pipeReaderStream.Read(buffer);

            public override int ReadByte() =>
                _pipeReaderStream.ReadByte();
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Socket is not supported on Browser")]
    public sealed class SocketsHttpHandlerTest_ConnectCallback_Http11 : SocketsHttpHandlerTest_ConnectCallback
    {
        public SocketsHttpHandlerTest_ConnectCallback_Http11(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_ConnectCallback_Http2 : SocketsHttpHandlerTest_ConnectCallback
    {
        public SocketsHttpHandlerTest_ConnectCallback_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public abstract class SocketsHttpHandlerTest_PlaintextStreamFilter : HttpClientHandlerTestBase
    {
        public SocketsHttpHandlerTest_PlaintextStreamFilter(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> PlaintextStreamFilter_ContextHasCorrectProperties_Success_MemberData() =>
            from useSsl in new[] { false, true }
            from syncRequest in new[] { false, true }
            from syncCallback in new[] { false, true }
            select new object[] { useSsl, syncRequest, syncCallback };

        [Theory]
        [MemberData(nameof(PlaintextStreamFilter_ContextHasCorrectProperties_Success_MemberData))]
        [SkipOnPlatform(TestPlatforms.Android, "Synchronous Send is not supported on Android")]
        public async Task PlaintextStreamFilter_ContextHasCorrectProperties_Success(bool useSsl, bool syncRequest, bool syncCallback)
        {
            if (syncRequest && UseVersion > HttpVersion.Version11)
            {
                // Sync requests are only supported on 1.x
                return;
            }

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = UseVersion;
                    requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = async (context, token) =>
                    {
                        Assert.Equal(UseVersion, context.NegotiatedHttpVersion);
                        Assert.Equal(requestMessage, context.InitialRequestMessage);

                        if (!syncCallback)
                        {
                            await Task.Delay(1); // to increase the chances of the whole operation completing asynchronously, without consuming too much additional time
                        }

                        return context.PlaintextStream;
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpResponseMessage response = await (syncRequest ?
                        Task.Run(() => client.Send(requestMessage)) :
                        client.SendAsync(requestMessage));
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                }, options: options);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PlaintextStreamFilter_SimpleDelegatingStream_Success(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = (context, token) =>
                    {
                        Assert.Equal(UseVersion, context.NegotiatedHttpVersion);

                        DelegateStream newStream = new DelegateStream(
                            canReadFunc: () => true,
                            canWriteFunc: () => true,
                            readAsyncFunc: context.PlaintextStream.ReadAsync,
                            writeAsyncFunc: context.PlaintextStream.WriteAsync,
                            disposeFunc: (disposing) => { if (disposing) { context.PlaintextStream.Dispose(); } });

                        return ValueTask.FromResult<Stream>(newStream);
                    };

                    using HttpClient client = CreateHttpClient(handler);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using HttpResponseMessage response = await client.GetAsync(uri);
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    HttpRequestData request = await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                    if (request.Version == HttpVersion20.Value)
                    {
                        HttpHeaderData schemeHeader = Assert.Single(request.Headers, headerData => headerData.Name == ":scheme");
                        Assert.Equal(useSsl ? "https" : "http", schemeHeader.Value);
                    }
                }, options: options);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PlaintextStreamFilter_ConnectionPrefix_Success(bool useSsl)
        {
            byte[] RequestPrefix = "request prefix\r\n"u8.ToArray();
            byte[] ResponsePrefix = "response prefix\r\n"u8.ToArray();

            using var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listenSocket.Listen();

            Task clientTask = Task.Run(async () =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                socketsHandler.PlaintextStreamFilter = async (context, token) =>
                {
                    await context.PlaintextStream.WriteAsync(RequestPrefix);

                    byte[] buffer = new byte[ResponsePrefix.Length];
                    await context.PlaintextStream.ReadAsync(buffer);
                    Assert.True(buffer.SequenceEqual(ResponsePrefix));

                    return context.PlaintextStream;
                };

                using HttpClient client = CreateHttpClient(handler);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                string response = await client.GetStringAsync($"{(useSsl ? "https" : "http")}://{listenSocket.LocalEndPoint}/foo");
                Assert.Equal("foo", response);
            });

            Task serverTask = Task.Run(async () =>
            {
                Socket serverSocket = await listenSocket.AcceptAsync();
                Stream serverStream = new NetworkStream(serverSocket, ownsSocket: true);

                if (useSsl)
                {
                    var sslStream = new SslStream(serverStream, false, delegate { return true; });

                    using (X509Certificate2 cert = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate())
                    {
                        SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();

                        options.EnabledSslProtocols = SslProtocols.Tls12;

                        var protocols = new List<SslApplicationProtocol>();
                        protocols.Add(SslApplicationProtocol.Http2);
                        options.ApplicationProtocols = protocols;

                        options.ServerCertificate = cert;

                        await sslStream.AuthenticateAsServerAsync(options, CancellationToken.None).ConfigureAwait(false);
                    }

                    serverStream = sslStream;
                }

                byte[] buffer = new byte[RequestPrefix.Length];
                await serverStream.ReadAsync(buffer);
                Assert.True(buffer.SequenceEqual(RequestPrefix));

                await serverStream.WriteAsync(ResponsePrefix);

                await using GenericLoopbackConnection loopbackConnection = await LoopbackServerFactory.CreateConnectionAsync(socket: null, serverStream, new GenericLoopbackOptions() { UseSsl = false });
                await loopbackConnection.InitializeConnectionAsync();

                HttpRequestData requestData = await loopbackConnection.ReadRequestDataAsync();
                Assert.Equal("/foo", requestData.Path);

                await loopbackConnection.SendResponseAsync(content: "foo");
            });

            await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PlaintextStreamFilter_ExceptionDuringCallback_ThrowsHttpRequestExceptionWithInnerException(bool useSsl)
        {
            Exception e = new Exception("hello!");

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = UseVersion;
                    requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = (context, token) =>
                    {
                        throw e;
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpRequestException hre = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.SendAsync(requestMessage));
                    Assert.Equal(e, hre.InnerException);
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                    }
                }, options: options);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PlaintextStreamFilter_ReturnsNull_ThrowsHttpRequestException(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = UseVersion;
                    requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = (context, token) =>
                    {
                        return ValueTask.FromResult<Stream>(null);
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpRequestException hre = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.SendAsync(requestMessage));
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                    }
                }, options: options);
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocketsHttpHandlerTest_PlaintextStreamFilter_Http11 : SocketsHttpHandlerTest_PlaintextStreamFilter
    {
        public SocketsHttpHandlerTest_PlaintextStreamFilter_Http11(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PlaintextStreamFilter_CustomStream_Success(bool useSsl)
        {
            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = (context, token) =>
                    {
                        Assert.Equal(UseVersion, context.NegotiatedHttpVersion);

                        context.PlaintextStream.Dispose();

                        MemoryStream memoryStream = new MemoryStream();
                        memoryStream.Write("HTTP/1.1 200 OK\r\nContent-Length: 3\r\n\r\nfoo"u8);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        DelegateStream newStream = new DelegateStream(
                            canReadFunc: () => true,
                            canWriteFunc: () => true,
                            readAsyncFunc: (buffer, offset, length, cancellationToken) => memoryStream.ReadAsync(buffer, offset, length, cancellationToken),
                            writeAsyncFunc: (buffer, offset, length, cancellationToken) => Task.CompletedTask);

                        return ValueTask.FromResult<Stream>(newStream);
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpResponseMessage response = await client.GetAsync(uri);
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    // Client intentionally disconnects. Ignore exception.
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                    }
                    catch (IOException) { }
                }, options: options);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PlaintextStreamFilter_Logging_Success(bool useSsl)
        {
            bool log = int.TryParse(Environment.GetEnvironmentVariable("DOTNET_TEST_SOCKETSHTTPHANDLERLOG"), out int value) && value == 1;

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = useSsl };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    string sendText = "";
                    string recvText = "";

                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.PlaintextStreamFilter = (context, token) =>
                    {
                        Assert.Equal(HttpVersion.Version11, context.NegotiatedHttpVersion);

                        static void Log(ref string text, bool log, string prefix, Stream stream, ReadOnlySpan<char> hex, ReadOnlySpan<char> ascii)
                        {
                            if (log) Console.WriteLine($"[{prefix} {stream.GetHashCode():X8}] {hex.ToString().PadRight(71)}  {ascii.ToString()}");
                            text += ascii.ToString();
                        }

                        return ValueTask.FromResult<Stream>(new BytesLoggingStream(
                            context.PlaintextStream,
                            (stream, hex, ascii) => Log(ref sendText, log, "SEND", stream, hex, ascii),
                            (stream, hex, ascii) => Log(ref recvText, log, "RECV", stream, hex, ascii)));
                    };

                    using HttpClient client = CreateHttpClient(handler);
                    using HttpResponseMessage response = await client.GetAsync(uri);
                    Assert.Equal("hello", await response.Content.ReadAsStringAsync());

                    Assert.Contains("GET / HTTP/1.1", sendText);
                    Assert.Contains("Host: ", sendText);

                    Assert.Contains("HTTP/1.1 200 OK", recvText);
                    Assert.Contains("hello", recvText);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello");
                }, options: options);
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_PlaintextStreamFilter_Http2 : SocketsHttpHandlerTest_PlaintextStreamFilter
    {
        public SocketsHttpHandlerTest_PlaintextStreamFilter_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_Cookies_Http2 : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Http2 : HttpClientHandlerTest
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http11 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http11(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNodeJS))]
        [InlineData("foo ", "bar ")]
        [InlineData("foo", " bar")]
        [InlineData("foo", "bar\t")]
        [InlineData("foo", "\tbar")]
        [InlineData("foo ", " bar ")]
        [InlineData("foo", "\tbar\t")]
        [InlineData("foo", "\t bar \t")]
        [InlineData("foo  ", " \t bar  \r\n ")]
        public async Task ResponseHeaders_ExtraWhitespace_Trimmed(string name, string value)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                using HttpResponseMessage response = await client.GetAsync(uri);

                Assert.True(response.Headers.NonValidated.TryGetValues("foo", out HeaderStringValues values));
                Assert.Equal("bar", Assert.Single(values));
            },
            async server =>
            {
                await server.HandleRequestAsync(headers: new[] { new HttpHeaderData(name, value) });
            });
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http2 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http2 : SocketsHttpHandler_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Http3 : HttpClientHandlerTest
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_Cookies_Http3 : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http3 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http3 : SocketsHttpHandler_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_AltSvc_Test_Http3 : HttpClientHandler_AltSvc_Test
    {
        public SocketsHttpHandler_HttpClientHandler_AltSvc_Test_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http3 : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public abstract class SocketsHttpHandler_RequestValidationTest
    {
        protected abstract bool TestAsync { get; }

        [Fact]
        public void Send_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("request", () =>
            {
                var invoker = new HttpMessageInvoker(new SocketsHttpHandler());
                if (TestAsync)
                {
                    invoker.SendAsync(null, CancellationToken.None);
                }
                else
                {
                    invoker.Send(null, CancellationToken.None);
                }
            });
        }

        [Fact]
        public void Send_NullRequestUri_ThrowsInvalidOperationException()
        {
            Throws<InvalidOperationException>(new HttpRequestMessage());
        }

        [Fact]
        public void Send_RelativeRequestUri_ThrowsInvalidOperationException()
        {
            Throws<InvalidOperationException>(new HttpRequestMessage(HttpMethod.Get, new Uri("/relative", UriKind.Relative)));
        }

        [Fact]
        public void Send_UnsupportedRequestUriScheme_ThrowsNotSupportedException()
        {
            Throws<NotSupportedException>(new HttpRequestMessage(HttpMethod.Get, "foo://foo.bar"));
        }

        [Fact]
        public void Send_MajorVersionZero_ThrowsNotSupportedException()
        {
            Throws<NotSupportedException>(new HttpRequestMessage { Version = new Version(0, 42) });
        }

        [Fact]
        public void Send_TransferEncodingChunkedWithNoContent_ThrowsHttpRequestException()
        {
            var request = new HttpRequestMessage();
            request.Headers.TransferEncodingChunked = true;

            HttpRequestException exception = Throws<HttpRequestException>(request);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Send_Http10WithTransferEncodingChunked_ThrowsNotSupportedException()
        {
            var request = new HttpRequestMessage
            {
                Content = new StringContent("foo"),
                Version = new Version(1, 0)
            };
            request.Headers.TransferEncodingChunked = true;

            Throws<NotSupportedException>(request);
        }

        private TException Throws<TException>(HttpRequestMessage request)
            where TException : Exception
        {
            var invoker = new HttpMessageInvoker(new SocketsHttpHandler());
            if (TestAsync)
            {
                Task<HttpResponseMessage> task = invoker.SendAsync(request, CancellationToken.None);
                Assert.Equal(TaskStatus.Faulted, task.Status);
                return Assert.IsType<TException>(task.Exception.InnerException);
            }
            else
            {
                return Assert.Throws<TException>(() => invoker.Send(request, CancellationToken.None));
            }
        }
    }

    public sealed class SocketsHttpHandler_RequestValidationTest_Async : SocketsHttpHandler_RequestValidationTest
    {
        protected override bool TestAsync => true;
    }

    public sealed class SocketsHttpHandler_RequestValidationTest_Sync : SocketsHttpHandler_RequestValidationTest
    {
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public abstract class SocketsHttpHandler_RequestContentLengthMismatchTest : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_RequestContentLengthMismatchTest(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public async Task ContentLength_DoesNotMatchRequestContentLength_Throws(int contentLength, int bytesSent)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using var client = CreateHttpClient();

                var content = new ByteArrayContent(new byte[bytesSent]);
                content.Headers.ContentLength = contentLength;

                Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync(uri, content));
                Assert.Contains("Content-Length", ex.Message);

                if (UseVersion.Major == 1)
                {
                    await client.GetStringAsync(uri).WaitAsync(TestHelper.PassingTestTimeout);
                }
            },
            async server =>
            {
                try
                {
                    await server.HandleRequestAsync();
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                }

                // On HTTP/1.x, an exception being thrown while sending the request content will result in the connection being closed.
                // This test is ensuring that a subsequent request can succeed on a new connection.
                if (UseVersion.Major == 1)
                {
                    await server.HandleRequestAsync().WaitAsync(TestHelper.PassingTestTimeout);
                }
            });
        }
    }

    public sealed class SocketsHttpHandler_RequestContentLengthMismatchTest_Http11 : SocketsHttpHandler_RequestContentLengthMismatchTest
    {
        public SocketsHttpHandler_RequestContentLengthMismatchTest_Http11(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandler_RequestContentLengthMismatchTest_Http2 : SocketsHttpHandler_RequestContentLengthMismatchTest
    {
        public SocketsHttpHandler_RequestContentLengthMismatchTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_RequestContentLengthMismatchTest_Http3 : SocketsHttpHandler_RequestContentLengthMismatchTest
    {
        public SocketsHttpHandler_RequestContentLengthMismatchTest_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public abstract class SocketsHttpHandler_SecurityTest : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_SecurityTest(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public async Task SslOptions_CustomTrust_Ok()
        {
            X509Certificate2Collection caCerts = new X509Certificate2Collection();
            X509Certificate2 certificate = Configuration.Certificates.GetDynamicServerCerttificate(caCerts);

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = true, Certificate = certificate };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: false);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);

                    var policy = new X509ChainPolicy()
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                    };

                    policy.ExtraStore.AddRange(caCerts);
                    policy.CustomTrustStore.Add(caCerts[caCerts.Count - 1]);
                    socketsHandler.SslOptions = new SslClientAuthenticationOptions() { CertificateChainPolicy = policy };
                    using HttpClient client = CreateHttpClient(handler);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
                    // This will drive SNI and name verification
                    request.Headers.Host = "localhost";
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                }, options: options);
        }

        [Fact]
        public async Task SslOptions_InvalidName_Throws()
        {
            X509Certificate2Collection caCerts = new X509Certificate2Collection();
            using X509Certificate2 certificate = Configuration.Certificates.GetDynamicServerCerttificate(caCerts);

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = true, Certificate = certificate };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: false);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                    using HttpClient client = CreateHttpClient(handler);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
                    // This will drive SNI and name verification
                    request.Headers.Host = Guid.NewGuid().ToString("N");

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, HttpCompletionOption.ResponseContentRead));
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                    }
                    catch { };
                }, options: options);
        }

        [Fact]
        public async Task SslOptions_CustomPolicy_IgnoresNameMismatch()
        {
            X509Certificate2Collection caCerts = new X509Certificate2Collection();
            X509Certificate2 certificate = Configuration.Certificates.GetDynamicServerCerttificate(caCerts);

            GenericLoopbackOptions options = new GenericLoopbackOptions() { UseSsl = true, Certificate = certificate };
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: false);
                    var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);

                    var policy = new X509ChainPolicy()
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        VerificationFlags = X509VerificationFlags.IgnoreInvalidName,
                    };

                    policy.ExtraStore.AddRange(caCerts);
                    policy.CustomTrustStore.Add(caCerts[caCerts.Count -1]);
                    socketsHandler.SslOptions = new SslClientAuthenticationOptions() { CertificateChainPolicy = policy };

                    using HttpClient client = CreateHttpClient(handler);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
                    // This will drive SNI and name verification
                    request.Headers.Host = Guid.NewGuid().ToString("N");

                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: "foo");
                }, options: options);
        }
    }

    public sealed class SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http11 : SocketsHttpHandler_SecurityTest
    {
        public SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http11(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version11;

#if DEBUG
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task Https_MultipleRequests_TlsResumed(bool useSocketHandler)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                HttpMessageHandler handler = useSocketHandler ? CreateSocketsHttpHandler(allowAllCertificates: true) : CreateHttpClientHandler();
                using (HttpClient client = CreateHttpClient(handler))
                {
                    HttpRequestMessage request = new  HttpRequestMessage(HttpMethod.Get,uri);
                    request.Headers.Add("Host", "foo.bar");
                    request.Headers.Add("Connection", "close");

                    HttpResponseMessage response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    request = new  HttpRequestMessage(HttpMethod.Get,uri);
                    request.Headers.Add("Host", "foo.bar");
                    response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            },
            async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync();
                await server.AcceptConnectionAsync(async connection =>
                {
                    SslStream ssl = (SslStream)connection.Stream;
                    object connectionInfo = typeof(SslStream).GetField(
                                     "_connectionInfo",
                                     BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ssl);

                    bool resumed = (bool)connectionInfo.GetType().GetProperty("TlsResumed").GetValue(connectionInfo);
                    Assert.True(resumed);

                    await connection.ReadRequestHeaderAndSendResponseAsync();
                });
            },
            new LoopbackServer.Options { UseSsl = true, SslProtocols = SslProtocols.Tls12 });
        }
#endif
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http2 : SocketsHttpHandler_SecurityTest
    {
        public SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http3 : SocketsHttpHandler_SecurityTest
    {
        public SocketsHttpHandler_SocketsHttpHandler_SecurityTest_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public abstract class SocketsHttpHandler_HttpRequestErrorTest : HttpClientHandlerTestBase
    {
        protected SocketsHttpHandler_HttpRequestErrorTest(ITestOutputHelper output) : base(output)
        {
        }

        // On Windows7 DNS may return SocketError.NoData (WSANO_DATA), which we currently don't map to NameResolutionError.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public async Task NameResolutionError()
        {
            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage message = new(HttpMethod.Get, new Uri("https://BadHost"))
            {
                Version = UseVersion,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(message));
            Assert.Equal(HttpRequestError.NameResolutionError, ex.HttpRequestError);
            Assert.IsType<SocketException>(ex.InnerException);
        }

        [Fact]
        public async Task ConnectionError()
        {
            if (UseVersion.Major == 3)
            {
                return;
            }
            using Socket notListening = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            notListening.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int port = ((IPEndPoint)notListening.LocalEndPoint).Port;
            Uri uri = new($"http://localhost:{port}");

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage message = new(HttpMethod.Get, uri)
            {
                Version = UseVersion,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(message));
            Assert.Equal(HttpRequestError.ConnectionError, ex.HttpRequestError);
        }

        [Fact]
        public async Task SecureConnectionError()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);
                GetUnderlyingSocketsHttpHandler(handler).SslOptions = new SslClientAuthenticationOptions()
                {
                    RemoteCertificateValidationCallback = delegate { return false; },
                };
                using HttpRequestMessage message = new(HttpMethod.Get, uri)
                {
                    Version = UseVersion,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(message));
                Assert.Equal(HttpRequestError.SecureConnectionError, ex.HttpRequestError);
            }, async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(_ => Task.CompletedTask);
                }
                catch
                {
                }
            },
            options: new GenericLoopbackOptions() { UseSsl = true });
        }

        
    }

    public sealed class SocketsHttpHandler_HttpRequestErrorTest_Http11 : SocketsHttpHandler_HttpRequestErrorTest
    {
        public SocketsHttpHandler_HttpRequestErrorTest_Http11(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandler_HttpRequestErrorTest_Http20 : SocketsHttpHandler_HttpRequestErrorTest
    {
        public SocketsHttpHandler_HttpRequestErrorTest_Http20(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;

        [Fact]
        public async Task VersionNegitioationError()
        {
            await Http11LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage message = new(HttpMethod.Get, uri)
                {
                    Version = UseVersion,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(message));
                Assert.Equal(HttpRequestError.VersionNegotiationError, ex.HttpRequestError);
            }, async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(_ => Task.CompletedTask);
                }
                catch
                {
                }
            },
            options: new GenericLoopbackOptions() { UseSsl = true });
        }
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpRequestErrorTest_Http30 : SocketsHttpHandler_HttpRequestErrorTest
    {
        public SocketsHttpHandler_HttpRequestErrorTest_Http30(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    public class MySsl : SslStream
    {
        public MySsl(Stream stream) : base(stream)
        {
        }
    }
}
