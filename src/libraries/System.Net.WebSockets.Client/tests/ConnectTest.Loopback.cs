// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class ConnectTest_Loopback(ITestOutputHelper output) : ConnectTestBase(output)
    {
        // --- Loopback Echo Server "overrides" ---

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task EchoBinaryMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoBinaryMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task EchoTextMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoTextMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_AddCustomHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_AddCustomHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_CookieHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_CookieHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol, useSsl);
    }

    // --- HTTP/1.1 WebSocket loopback tests ---

    public abstract class ConnectTest_Loopback_Http11(ITestOutputHelper output) : ConnectTest_Loopback(output)
    {
        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState, useSsl);
    }

    public sealed class ConnectTest_Invoker_Loopback(ITestOutputHelper output) : ConnectTest_Loopback_Http11(output)
    {
        protected override bool UseCustomInvoker => true;

        public static IEnumerable<object[]> ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException_MemberData()
        {
            yield return Throw(options => options.UseDefaultCredentials = true);
            yield return NoThrow(options => options.UseDefaultCredentials = false);
            yield return Throw(options => options.Credentials = new NetworkCredential());
            yield return Throw(options => options.Proxy = new WebProxy());

            // Will result in an exception on apple mobile platforms
            // and crash the test.
            if (PlatformDetection.IsNotAppleMobile)
            {
                yield return Throw(options => options.ClientCertificates.Add(Test.Common.Configuration.Certificates.GetClientCertificate()));
            }

            yield return NoThrow(options => options.ClientCertificates = new X509CertificateCollection());
            yield return Throw(options => options.RemoteCertificateValidationCallback = delegate { return true; });
            yield return Throw(options => options.Cookies = new CookieContainer());

            // We allow no proxy or the default proxy to be used
            yield return NoThrow(options => { });
            yield return NoThrow(options => options.Proxy = null);

            // These options don't conflict with the custom invoker
            yield return NoThrow(options => options.HttpVersion = new Version(2, 0));
            yield return NoThrow(options => options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher);
            yield return NoThrow(options => options.SetRequestHeader("foo", "bar"));
            yield return NoThrow(options => options.AddSubProtocol("foo"));
            yield return NoThrow(options => options.KeepAliveInterval = TimeSpan.FromSeconds(42));
            yield return NoThrow(options => options.DangerousDeflateOptions = new WebSocketDeflateOptions());
            yield return NoThrow(options => options.CollectHttpResponseDetails = true);

            static object[] Throw(Action<ClientWebSocketOptions> configureOptions) =>
                new object[] { configureOptions, true };

            static object[] NoThrow(Action<ClientWebSocketOptions> configureOptions) =>
                new object[] { configureOptions, false };
        }

        [Theory]
        [MemberData(nameof(ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "Custom invoker is ignored on Browser")]
        public async Task ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException(Action<ClientWebSocketOptions> configureOptions, bool shouldThrow)
        {
            using var invoker = new HttpMessageInvoker(new SocketsHttpHandler
            {
                ConnectCallback = (_, _) => ValueTask.FromException<Stream>(new Exception("ConnectCallback"))
            });

            using var ws = new ClientWebSocket();
            configureOptions(ws.Options);

            Task connectTask = ws.ConnectAsync(new Uri("wss://dummy"), invoker, CancellationToken.None);
            if (shouldThrow)
            {
                Assert.Equal(TaskStatus.Faulted, connectTask.Status);
                await Assert.ThrowsAsync<ArgumentException>("options", () => connectTask);
            }
            else
            {
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() => connectTask);
                Assert.NotNull(ex.InnerException);
                Assert.Contains("ConnectCallback", ex.InnerException.Message);
            }

            foreach (X509Certificate cert in ws.Options.ClientCertificates)
            {
                cert.Dispose();
            }
        }
    }

    public sealed class ConnectTest_HttpClient_Loopback(ITestOutputHelper output) : ConnectTest_Loopback_Http11(output)
    {
        protected override bool UseHttpClient => true;
    }
}
