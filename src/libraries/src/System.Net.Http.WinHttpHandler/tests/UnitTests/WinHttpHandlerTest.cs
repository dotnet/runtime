// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpHandlerTest : IDisposable
    {
        private const string ExpectedResponseBody = "This is the response body.";
        private const string FakeServerEndpoint = "http://www.contoso.com/";
        private const string FakeSecureServerEndpoint = "https://www.contoso.com/";
        private const string FakeProxy = "http://proxy.contoso.com";

        public WinHttpHandlerTest()
        {
            TestControl.ResetAll();
        }

        public void Dispose()
        {
            TestControl.ResponseDelayCompletedEvent.WaitOne();
            
            FakeSafeWinHttpHandle.ForceGarbageCollection();
            Assert.Equal(0, FakeSafeWinHttpHandle.HandlesOpen);
        }

        [Fact]
        public void AutomaticRedirection_CtorAndGet_DefaultValueIsTrue()
        {
            var handler = new WinHttpHandler();
            
            Assert.True(handler.AutomaticRedirection);
        }

        [Fact]
        public void AutomaticRedirection_SetFalseAndGet_ValueIsFalse()
        {
            var handler = new WinHttpHandler();
            handler.AutomaticRedirection = false;
            
            Assert.False(handler.AutomaticRedirection);
        }

        [Fact]
        public void AutomaticRedirection_SetTrue_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate { handler.AutomaticRedirection = true; });

            Assert.Equal(
                Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY_DISALLOW_HTTPS_TO_HTTP,
                APICallHistory.WinHttpOptionRedirectPolicy);
        }

        [Fact]
        public void AutomaticRedirection_SetFalse_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate { handler.AutomaticRedirection = false; });

            Assert.Equal(
                Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY_NEVER,
                APICallHistory.WinHttpOptionRedirectPolicy);
        }

        [Fact]
        public void CheckCertificateRevocationList_SetTrue_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(handler, delegate { handler.CheckCertificateRevocationList = true; });

            Assert.True(APICallHistory.WinHttpOptionEnableSslRevocation.Value);
        }

        [Fact]
        public void CheckCertificateRevocationList_SetFalse_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(handler, delegate { handler.CheckCertificateRevocationList = false; });

            Assert.Equal(false, APICallHistory.WinHttpOptionEnableSslRevocation.HasValue);
        }

        [Fact]
        public void ConnectTimeout_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.ConnectTimeout = TimeSpan.FromMinutes(-10); });
        }

        [Fact]
        public void ConnectTimeout_SetTooLargeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ConnectTimeout = TimeSpan.FromMilliseconds(int.MaxValue + 1.0); });
        }

        [Fact]
        public void ConnectTimeout_SetZeroValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.ConnectTimeout = TimeSpan.FromSeconds(0); });
        }

        [Fact]
        public void ConnectTimeout_SetInfiniteValue_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.ConnectTimeout = Timeout.InfiniteTimeSpan;
        }

        [Fact]
        public void CookieContainer_WhenCreated_ReturnsNull()
        {
            var handler = new WinHttpHandler();

            Assert.Null(handler.CookieContainer);
        }

        [Fact]
        public async Task CookieUsePolicy_UseSpecifiedCookieContainerAndNullContainer_ThrowsInvalidOperationException()
        {
            var handler = new WinHttpHandler();
            Assert.Null(handler.CookieContainer);
            handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
            var client = new HttpClient(handler);

            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        }

        [Fact]
        public void CookieUsePolicy_SetUsingInvalidEnum_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.CookieUsePolicy = (CookieUsePolicy)100; });
        }

        [Fact]
        public void CookieUsePolicy_WhenCreated_ReturnsUseInternalCookieStoreOnly()
        {
            var handler = new WinHttpHandler();

            Assert.Equal(CookieUsePolicy.UseInternalCookieStoreOnly, handler.CookieUsePolicy);
        }

        [Fact]
        public void CookieUsePolicy_SetIgnoreCookies_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.CookieUsePolicy = CookieUsePolicy.IgnoreCookies;
        }

        [Fact]
        public void CookieUsePolicy_SetUseInternalCookieStoreOnly_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.CookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly;
        }

        [Fact]
        public void CookieUsePolicy_SetUseSpecifiedCookieContainer_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
        }


        [Fact]
        public void CookieUsePolicy_SetIgnoreCookies_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(handler, delegate { handler.CookieUsePolicy = CookieUsePolicy.IgnoreCookies; });

            Assert.True(APICallHistory.WinHttpOptionDisableCookies.Value);
        }

        [Fact]
        public void CookieUsePolicy_SetUseInternalCookieStoreOnly_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate { handler.CookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly; });

            Assert.Equal(false, APICallHistory.WinHttpOptionDisableCookies.HasValue);
        }

        [Fact]
        [ActiveIssue(2165, PlatformID.AnyUnix)]
        public void CookieUsePolicy_SetUseSpecifiedCookieContainerAndContainer_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate {
                    handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
                    handler.CookieContainer = new CookieContainer();
                });

            Assert.Equal(true, APICallHistory.WinHttpOptionDisableCookies.HasValue);
        }

        [Fact]
        public void WindowsProxyUsePolicy_SetUsingInvalidEnum_ThrowArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.WindowsProxyUsePolicy = (WindowsProxyUsePolicy)100; });
        }

        [Fact]
        public void WindowsProxyUsePolicy_SetDoNotUseProxy_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy;
        }

        [Fact]
        public void WindowsProxyUsePolicy_SetUseWinHttpProxy_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinHttpProxy;
        }

        [Fact]
        public void WindowsProxyUsePolicy_SetUseWinWinInetProxy_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
        }

        [Fact]
        public void WindowsProxyUsePolicy_SetUseCustomProxy_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
        }

        [Fact]
        public async Task WindowsProxyUsePolicy_UseNonNullProxyAndIncorrectWindowsProxyUsePolicy_ThrowsInvalidOperationException()
        {
            var handler = new WinHttpHandler();
            handler.Proxy = new CustomProxy(false);
            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy;
            var client = new HttpClient(handler);

            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        }


        [Fact]
        public async Task WindowsProxyUsePolicy_UseCustomProxyAndNullProxy_ThrowsInvalidOperationException()
        {
            var handler = new WinHttpHandler();
            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
            handler.Proxy = null;
            var client = new HttpClient(handler);

            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        }

        [Fact]
        public void MaxAutomaticRedirections_SetZero_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();
            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.MaxAutomaticRedirections = 0; });
        }

        [Fact]
        public void MaxAutomaticRedirections_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();
            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.MaxAutomaticRedirections = -1; });
        }

        [Fact]
        public void MaxAutomaticRedirections_SetValidValue_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();
            int redirections = 35;

            SendRequestHelper(handler, delegate { handler.MaxAutomaticRedirections = redirections; });

            Assert.Equal((uint)redirections, APICallHistory.WinHttpOptionMaxHttpAutomaticRedirects);
        }

        [Fact]
        public void MaxConnectionsPerServer_SetZero_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();
            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.MaxConnectionsPerServer = 0; });
        }

        [Fact]
        public void MaxConnectionsPerServer_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();
            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.MaxConnectionsPerServer = -1; });
        }

        [Fact]
        public void MaxConnectionsPerServer_SetPositiveValue_Success()
        {
            var handler = new WinHttpHandler();
            handler.MaxConnectionsPerServer = 1;
        }
        
        [Fact]
        public void ReceiveDataTimeout_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ReceiveDataTimeout = TimeSpan.FromMinutes(-10); });
        }

        [Fact]
        public void ReceiveDataTimeout_SetTooLargeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ReceiveDataTimeout = TimeSpan.FromMilliseconds(int.MaxValue + 1.0); });
        }

        [Fact]
        public void ReceiveDataTimeout_SetZeroValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.ReceiveDataTimeout = TimeSpan.FromSeconds(0); });
        }

        [Fact]
        public void ReceiveDataTimeout_SetInfiniteValue_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.ReceiveDataTimeout = Timeout.InfiniteTimeSpan;
        }

        [Fact]
        public void ReceiveHeadersTimeout_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ReceiveHeadersTimeout = TimeSpan.FromMinutes(-10); });
        }

        [Fact]
        public void ReceiveHeadersTimeout_SetTooLargeValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ReceiveHeadersTimeout = TimeSpan.FromMilliseconds(int.MaxValue + 1.0); });
        }

        [Fact]
        public void ReceiveHeadersTimeout_SetZeroValue_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => { handler.ReceiveHeadersTimeout = TimeSpan.FromSeconds(0); });
        }

        [Fact]
        public void ReceiveHeadersTimeout_SetInfiniteValue_NoExceptionThrown()
        {
            var handler = new WinHttpHandler();

            handler.ConnectTimeout = Timeout.InfiniteTimeSpan;
        }

        [Fact]
        public void SslProtocols_SetUsingSsl2_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.SslProtocols = SslProtocols.Ssl2; });
        }

        [Fact]
        public void SslProtocols_SetUsingSsl3_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.SslProtocols = SslProtocols.Ssl3; });
        }

        [Fact]
        public void SslProtocols_SetUsingNone_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.SslProtocols = SslProtocols.None; });
        }

        [Fact]
        public void SslProtocols_SetUsingInvalidEnum_ThrowsArgumentOutOfRangeException()
        {
            var handler = new WinHttpHandler();

            Assert.Throws<ArgumentOutOfRangeException>(() => { handler.SslProtocols = (SslProtocols)4096; });
        }

        [Fact]
        public void SslProtocols_SetUsingValidEnums_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.SslProtocols =
                        SslProtocols.Tls |
                        SslProtocols.Tls11 |
                        SslProtocols.Tls12;
                });

            uint expectedProtocols =
                Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1 |
                Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_1 |
                Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_2;
            Assert.Equal(expectedProtocols, APICallHistory.WinHttpOptionSecureProtocols);
        }

        [Fact]
        public async Task GetAsync_MultipleRequestsReusingSameClient_Success()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            HttpResponseMessage response = await client.GetAsync(FakeServerEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync(FakeServerEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync(FakeServerEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            client.Dispose();
        }

        [Fact]
        public async Task SendAsync_PostContentWithContentLengthAndChunkedEncodingHeaders_Success()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TransferEncodingChunked = true;
            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            var content = new StringContent(ExpectedResponseBody);
            Assert.True(content.Headers.ContentLength.HasValue);
            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);
            request.Content = content;

            HttpResponseMessage response = await client.SendAsync(request);
        }

        [Fact]
        public async Task SendAsync_PostNoContentObjectWithChunkedEncodingHeader_ExpectInvalidOperationException()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TransferEncodingChunked = true;
            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        }

        // TODO: Need to skip this test due to missing native dependency clrcompression.dll.
        // https://github.com/dotnet/corefx/issues/1298
        [Fact]
        [ActiveIssue(1298)]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsDeflateCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            HttpResponseMessage response = SendRequestHelper(
                handler,
                delegate
                {
                    handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    TestServer.SetResponse(DecompressionMethods.Deflate, ExpectedResponseBody);
                });

            Assert.Null(response.Content.Headers.ContentLength);
            string responseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(0, response.Content.Headers.ContentEncoding.Count);
            Assert.Equal(ExpectedResponseBody, responseBody);
        }

        // TODO: Need to skip this test due to missing native dependency clrcompression.dll.
        // https://github.com/dotnet/corefx/issues/1298
        [Fact]
        [ActiveIssue(1298)]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsGZipCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            HttpResponseMessage response = SendRequestHelper(
                handler,
                delegate
                {
                    handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    TestServer.SetResponse(DecompressionMethods.GZip, ExpectedResponseBody);
                });

            Assert.Null(response.Content.Headers.ContentLength);
            string responseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(0, response.Content.Headers.ContentEncoding.Count);
            Assert.Equal(ExpectedResponseBody, responseBody);
        }

        [Fact]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsNotCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            HttpResponseMessage response = SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.NotNull(response.Content.Headers.ContentLength);
            string responseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(ExpectedResponseBody, responseBody);
        }

        [Fact]
        public void SendAsync_AutomaticProxySupportAndUseWinInetSettings_ExpectedWinHttpSessionProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, APICallHistory.SessionProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithAutoDetectSetting_ExpectedWinHttpProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = false;
            FakeRegistry.WinInetProxySettings.AutoDetect = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY, APICallHistory.RequestProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithEmptySettings_ExpectedWinHttpProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = false;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(false, APICallHistory.RequestProxySettings.AccessType.HasValue);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithManualSettingsOnly_ExpectedWinHttpProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = false;
            FakeRegistry.WinInetProxySettings.Proxy = FakeProxy;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(false, APICallHistory.RequestProxySettings.AccessType.HasValue);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithMissingRegistrySettings_ExpectedWinHttpProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = false;
            FakeRegistry.WinInetProxySettings.RegistryKeyMissing = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(false, APICallHistory.RequestProxySettings.AccessType.HasValue);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithAutoDetectButPACFileNotDetectedOnNetwork_ExpectedWinHttpProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = false;
            TestControl.PACFileNotDetectedOnNetwork = true;
            FakeRegistry.WinInetProxySettings.AutoDetect = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.RequestProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_NoAutomaticProxySupportAndUseWinInetSettingsWithAutoDetectSettingAndManualSettingButPACFileNotFoundOnNetwork_ExpectedWinHttpProxySettings()
        {
            const string manualProxy = FakeProxy;
            TestControl.WinHttpAutomaticProxySupport = false;
            FakeRegistry.WinInetProxySettings.AutoDetect = true;
            FakeRegistry.WinInetProxySettings.Proxy = manualProxy;
            TestControl.PACFileNotDetectedOnNetwork = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            // Both AutoDetect and manual proxy are specified.  If AutoDetect fails to find
            // the PAC file on the network, then we should fall back to manual setting.
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY, APICallHistory.RequestProxySettings.AccessType);
            Assert.Equal(manualProxy, APICallHistory.RequestProxySettings.Proxy);
        }

        [Fact]
        public void SendAsync_UseNoProxy_ExpectedWinHttpProxySettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper(handler, delegate { handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy; });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_UseCustomProxyWithNoBypass_ExpectedWinHttpProxySettings()
        {
            var handler = new WinHttpHandler();
            var customProxy = new CustomProxy(false);

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    handler.Proxy = customProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY, APICallHistory.RequestProxySettings.AccessType);
            Assert.Equal(FakeProxy, APICallHistory.RequestProxySettings.Proxy);
        }

        [Fact]
        public void SendAsync_UseCustomProxyWithBypass_ExpectedWinHttpProxySettings()
        {
            var handler = new WinHttpHandler();
            var customProxy = new CustomProxy(true);

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    handler.Proxy = customProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.RequestProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_AutomaticProxySupportAndUseDefaultWebProxy_ExpectedWinHttpSessionProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = true;
            var handler = new WinHttpHandler();

            SendRequestHelper(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    handler.Proxy = new FakeDefaultWebProxy();
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, APICallHistory.SessionProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_SecureRequestToServerWithNoCertificates_ExpectZeroIntPtrClientCertContext()
        {
            var handler = new WinHttpHandler();
            SendRequestHelper(
                handler,
                () => { },
                FakeSecureServerEndpoint);

            Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
            Assert.Equal(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
        }

        [Fact]
        public void SendAsync_NonSecureRequestToServer_ExpectNoClientCertContext()
        {
            var handler = new WinHttpHandler();
            SendRequestHelper(
                handler,
                () => { });

            Assert.Equal(0, APICallHistory.WinHttpOptionClientCertContext.Count);
        }

        [Fact]
        public async Task SendAsync_SlowPostRequestWithTimedCancellation_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            TestControl.ResponseDelayTime = 500;
            CancellationTokenSource cts = new CancellationTokenSource(100);
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, FakeServerEndpoint);
            var content = new StringContent(new String('a', 1000));
            request.Content = content;
            
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
        }

        [Fact]
        public async Task SendAsync_SlowGetRequestWithTimedCancellation_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            TestControl.ResponseDelayTime = 500;
            CancellationTokenSource cts = new CancellationTokenSource(100);
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, FakeServerEndpoint);
            
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
        }

        [Fact]
        public async Task SendAsync_RequestWithCanceledToken_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, FakeServerEndpoint);
            
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
        }

        [Fact]
        public async Task SendAsync_WinHttpOpenReturnsError_ExpectHttpRequestException()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, FakeServerEndpoint);

            TestControl.Fail.WinHttpOpen = true;

            Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
            Assert.Equal(typeof(WinHttpException), ex.InnerException.GetType());
        }
        
        [Fact]
        public void SendAsync_MultipleCallsWithDispose_NoHandleLeaks()
        {
            WinHttpHandler handler;
            HttpResponseMessage response;
            for (int i = 0; i < 50; i++)
            {
                handler = new WinHttpHandler();
                response = SendRequestHelper(handler, () => { });
                response.Dispose();
                handler.Dispose();
            }
            
            FakeSafeWinHttpHandle.ForceGarbageCollection();
            
            Assert.Equal(0, FakeSafeWinHttpHandle.HandlesOpen);
        }
        
        [Fact]
        public void SendAsync_MultipleCallsWithoutDispose_NoHandleLeaks()
        {
            WinHttpHandler handler;
            HttpResponseMessage response;
            for (int i = 0; i < 50; i++)
            {
                handler = new WinHttpHandler();
                response = SendRequestHelper(handler, () => { });
            }

            handler = null;
            response = null;
            FakeSafeWinHttpHandle.ForceGarbageCollection();
            
            Assert.Equal(0, FakeSafeWinHttpHandle.HandlesOpen);
        }
        
        private HttpResponseMessage SendRequestHelper(WinHttpHandler handler, Action setup)
        {
            return SendRequestHelper(handler, setup, FakeServerEndpoint);
        }

        private HttpResponseMessage SendRequestHelper(WinHttpHandler handler, Action setup, string fakeServerEndpoint)
        {
            TestServer.SetResponse(DecompressionMethods.None, ExpectedResponseBody);

            setup();

            var invoker = new HttpMessageInvoker(handler, false);
            var request = new HttpRequestMessage(HttpMethod.Get, fakeServerEndpoint);
            Task<HttpResponseMessage> task = invoker.SendAsync(request, CancellationToken.None);
            
            return task.GetAwaiter().GetResult();
        }
        
        public class CustomProxy : IWebProxy
        {
            private const string DefaultDomain = "domain";
            private const string DefaultUsername = "username";
            private const string DefaultPassword = "password";
            private bool bypassAll;
            private NetworkCredential networkCredential;

            public CustomProxy(bool bypassAll)
            {
                this.bypassAll = bypassAll;
                this.networkCredential = new NetworkCredential(CustomProxy.DefaultUsername, CustomProxy.DefaultPassword, CustomProxy.DefaultDomain);
            }

            public string UsernameWithDomain
            {
                get
                {
                    return CustomProxy.DefaultDomain + "\\" + CustomProxy.DefaultUsername;
                }
            }

            public string Password
            {
                get
                {
                    return CustomProxy.DefaultPassword;
                }
            }

            public NetworkCredential NetworkCredential
            {
                get
                {
                    return this.networkCredential;
                }
            }

            ICredentials IWebProxy.Credentials
            {
                get
                {
                    return this.networkCredential;
                }

                set
                {
                }
            }

            Uri IWebProxy.GetProxy(Uri destination)
            {
                return new Uri(FakeProxy);
            }

            bool IWebProxy.IsBypassed(Uri host)
            {
                return this.bypassAll;
            }
        }
        
        public class FakeDefaultWebProxy : IWebProxy
        {
            private ICredentials _credentials = null;

            public FakeDefaultWebProxy()
            {
            }

            public ICredentials Credentials
            {
                get
                {
                    return _credentials;
                }
                set
                {
                    _credentials = value;
                }
            }
            
            // This is a sentinel object representing the internal default system proxy that a developer would
            // use when accessing the System.Net.WebRequest.DefaultWebProxy property (from the System.Net.Requests
            // package). It can't support the GetProxy or IsBypassed methods. WinHttpHandler will handle this
            // exception and use the appropriate system default proxy.
            public Uri GetProxy(Uri destination)
            {
                throw new PlatformNotSupportedException();
            }

            public bool IsBypassed(Uri host)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
