// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// NOTE:
// Currently the managed handler is opt-in on both Windows and Unix, due to still being a nascent implementation
// that's missing features, robustness, perf, etc.  One opts into it currently by setting an environment variable,
// which makes it a bit difficult to test.  There are two straightforward ways to test it:
// - This file contains test classes that derive from the other test classes in the project that create
//   HttpClient{Handler} instances, and in the ctor sets the env var and in Dispose removes the env var.
//   That has the effect of running all of those same tests again, but with the managed handler enabled.
// - By setting the env var prior to running tests, every test will implicitly use the managed handler,
//   at which point the tests in this file are duplicative and can be commented out.

// For now parallelism is disabled because we use an env var to turn on the managed handler, and the env var
// impacts any tests running concurrently in the process.  We can remove this restriction in the future once
// plans around the ManagedHandler are better understood.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace System.Net.Http.Functional.Tests
{
    public sealed class ManagedHandler_HttpClientTest : HttpClientTest, IDisposable
    {
        public ManagedHandler_HttpClientTest() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_DiagnosticsTest : DiagnosticsTest, IDisposable
    {
        public ManagedHandler_DiagnosticsTest() => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    // TODO #23139: Tests on this class fail when the associated condition is enabled.
    //public sealed class ManagedHandler_HttpClientEKUTest : HttpClientEKUTest, IDisposable
    //{
    //    public ManagedHandler_HttpClientEKUTest() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    public sealed class ManagedHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    public sealed class ManagedHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_DefaultProxyCredentials_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    public sealed class ManagedHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_MaxConnectionsPerServer_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_ServerCertificates_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    public sealed class ManagedHandler_PostScenarioTest : PostScenarioTest, IDisposable
    {
        public ManagedHandler_PostScenarioTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_ResponseStreamTest : ResponseStreamTest, IDisposable
    {
        public ManagedHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }
    
    public sealed class ManagedHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_SslProtocols_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest, IDisposable
    {
        public ManagedHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_HttpClientMiniStress : HttpClientMiniStress, IDisposable
    {
        public ManagedHandler_HttpClientMiniStress() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    // TODO #23140:
    //public sealed class ManagedHandler_DefaultCredentialsTest : DefaultCredentialsTest, IDisposable
    //{
    //    public ManagedHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    public sealed class ManagedHandler_HttpClientHandlerTest : HttpClientHandlerTest, IDisposable
    {
        public ManagedHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    // TODO #23141: Socket's don't support canceling individual operations, so ReadStream on NetworkStream
    // isn't cancelable once the operation has started.  We either need to wrap the operation with one that's
    // "cancelable", meaning that the underlying operation will still be running even though we've returned "canceled",
    // or we need to just recognize that cancellation in such situations can be left up to the caller to do the
    // same thing if it's really important.
    //public sealed class ManagedHandler_CancellationTest : CancellationTest, IDisposable
    //{
    //    public ManagedHandler_CancellationTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    // TODO #23142: The managed handler doesn't currently track how much data was written for the response headers.
    //public sealed class ManagedHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test, IDisposable
    //{
    //    public ManagedHandler_HttpClientHandler_MaxResponseHeadersLength_Test() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public new void Dispose()
    //    {
    //        ManagedHandlerTestHelpers.RemoveEnvVar();
    //        base.Dispose();
    //    }
    //}

    public sealed class ManagedHandler_HttpClientHandler_ConnectionPooling_Test : IDisposable
    {
        public ManagedHandler_HttpClientHandler_ConnectionPooling_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();

        // TODO: Currently the subsequent tests sometimes fail/hang with WinHttpHandler / CurlHandler.
        // In theory they should pass with any handler that does appropriate connection pooling.
        // We should understand why they sometimes fail there and ideally move them to be
        // used by all handlers this test project tests.

        [Fact]
        public async Task MultipleIterativeRequests_SameConnectionReused()
        {
            using (var client = new HttpClient())
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
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
                    await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await firstRequest;

                    Task<Socket> secondAccept = listener.AcceptAsync(); // shouldn't complete

                    Task<string> additionalRequest = client.GetStringAsync(uri);
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
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
            using (var client = new HttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(100);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "\r\n";

                // Make multiple requests iteratively.
                for (int i = 0; i < 2; i++)
                {
                    Task<string> request = client.GetStringAsync(uri);
                    using (Socket server = await listener.AcceptAsync())
                    using (var serverStream = new NetworkStream(server, ownsSocket: false))
                    using (var serverReader = new StreamReader(serverStream))
                    {
                        while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
                        await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                        await request;

                        server.Shutdown(SocketShutdown.Both);
                        if (i == 0)
                        {
                            await Task.Delay(2000); // give client time to see the closing before next connect
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task ServerSendsConnectionClose_SubsequentRequestUsesDifferentConnection()
        {
            using (var client = new HttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(100);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "Connection: close\r\n" +
                    "\r\n";

                // Make multiple requests iteratively.
                Task<string> request1 = client.GetStringAsync(uri);
                using (Socket server1 = await listener.AcceptAsync())
                using (var serverStream1 = new NetworkStream(server1, ownsSocket: false))
                using (var serverReader1 = new StreamReader(serverStream1))
                {
                    while (!string.IsNullOrWhiteSpace(await serverReader1.ReadLineAsync()));
                    await server1.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await request1;

                    Task<string> request2 = client.GetStringAsync(uri);
                    using (Socket server2 = await listener.AcceptAsync())
                    using (var serverStream2 = new NetworkStream(server2, ownsSocket: false))
                    using (var serverReader2 = new StreamReader(serverStream2))
                    {
                        while (!string.IsNullOrWhiteSpace(await serverReader2.ReadLineAsync()));
                        await server2.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                        await request2;
                    }
                }
            }
        }
    }
}
