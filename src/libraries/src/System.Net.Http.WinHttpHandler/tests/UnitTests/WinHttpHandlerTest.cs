// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpHandlerTest
    {
        private const string FakeProxy = "http://proxy.contoso.com";
        
        private readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
            TestControl.ResetAll();
        }

        [Fact]
        public void Ctor_ExpectedDefaultPropertyValues()
        {
            var handler = new WinHttpHandler();

            Assert.Equal(SslProtocols.None, handler.SslProtocols);
            Assert.Equal(true, handler.AutomaticRedirection);
            Assert.Equal(50, handler.MaxAutomaticRedirections);
            Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
            Assert.Equal(CookieUsePolicy.UseInternalCookieStoreOnly, handler.CookieUsePolicy);
            Assert.Equal(null, handler.CookieContainer);
            Assert.Equal(null, handler.ServerCertificateValidationCallback);
            Assert.Equal(false, handler.CheckCertificateRevocationList);
            Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOption);
            X509Certificate2Collection certs = handler.ClientCertificates;
            Assert.True(certs.Count == 0);
            Assert.Equal(false, handler.PreAuthenticate);
            Assert.Equal(null, handler.ServerCredentials);
            Assert.Equal(WindowsProxyUsePolicy.UseWinHttpProxy, handler.WindowsProxyUsePolicy);
            Assert.Equal(null, handler.DefaultProxyCredentials);
            Assert.Equal(null, handler.Proxy);
            Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
            Assert.Equal(TimeSpan.FromSeconds(30), handler.SendTimeout);
            Assert.Equal(TimeSpan.FromSeconds(30), handler.ReceiveHeadersTimeout);
            Assert.Equal(TimeSpan.FromSeconds(30), handler.ReceiveDataTimeout);
            Assert.Equal(64, handler.MaxResponseHeadersLength);
            Assert.Equal(64 * 1024, handler.MaxResponseDrainSize);
            Assert.NotNull(handler.Properties);
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(handler, delegate { handler.CheckCertificateRevocationList = true; });

            Assert.True(APICallHistory.WinHttpOptionEnableSslRevocation.Value);
        }

        [Fact]
        public void CheckCertificateRevocationList_SetFalse_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper.Send(handler, delegate { handler.CheckCertificateRevocationList = false; });

            Assert.Equal(false, APICallHistory.WinHttpOptionEnableSslRevocation.HasValue);
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
            using (var client = new HttpClient(handler))
            {
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            }
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

            SendRequestHelper.Send(handler, delegate { handler.CookieUsePolicy = CookieUsePolicy.IgnoreCookies; });

            Assert.True(APICallHistory.WinHttpOptionDisableCookies.Value);
        }

        [Fact]
        public void CookieUsePolicy_SetUseInternalCookieStoreOnly_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper.Send(
                handler,
                delegate { handler.CookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly; });

            Assert.Equal(false, APICallHistory.WinHttpOptionDisableCookies.HasValue);
        }

        [Fact]
        public void CookieUsePolicy_SetUseSpecifiedCookieContainerAndContainer_ExpectedWinHttpHandleSettings()
        {
            var handler = new WinHttpHandler();

            SendRequestHelper.Send(
                handler,
                delegate {
                    handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
                    handler.CookieContainer = new CookieContainer();
                });

            Assert.Equal(true, APICallHistory.WinHttpOptionDisableCookies.HasValue);
        }

        [Fact]
        public void Properties_Get_CountIsZero()
        {
            var handler = new WinHttpHandler();
            IDictionary<string, object> dict = handler.Properties;
            Assert.Equal(0, dict.Count);
        }

        [Fact]
        public void Properties_AddItemToDictionary_ItemPresent()
        {
            var handler = new WinHttpHandler();
            IDictionary<string, object> dict = handler.Properties;
            Assert.Same(dict, handler.Properties);

            var item = new object();
            dict.Add("item", item);

            object value;
            Assert.True(dict.TryGetValue("item", out value));
            Assert.Equal(item, value);
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
            using (var client = new HttpClient(handler))
            {
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            }
        }


        [Fact]
        public async Task WindowsProxyUsePolicy_UseCustomProxyAndNullProxy_ThrowsInvalidOperationException()
        {
            var handler = new WinHttpHandler();
            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
            handler.Proxy = null;
            using (var client = new HttpClient(handler))
            {
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            }
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

            SendRequestHelper.Send(handler, delegate { handler.MaxAutomaticRedirections = redirections; });

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

            handler.ReceiveHeadersTimeout = Timeout.InfiniteTimeSpan;
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public void SslProtocols_SetUsingSupported_Success(SslProtocols protocol)
        {
            var handler = new WinHttpHandler();
            handler.SslProtocols = protocol;
        }

        [Fact]
        public void SslProtocols_SetUsingNone_Success()
        {
            var handler = new WinHttpHandler();

            handler.SslProtocols = SslProtocols.None;
        }

        [Theory]
        [InlineData(
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
            Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1 |
            Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_1 |
            Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_2)]
#pragma warning disable 0618
        [InlineData(
            SslProtocols.Ssl2 | SslProtocols.Ssl3,
            Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_SSL2 |
            Interop.WinHttp.WINHTTP_FLAG_SECURE_PROTOCOL_SSL3)]
#pragma warning restore 0618
        public void SslProtocols_SetUsingValidEnums_ExpectedWinHttpHandleSettings(
            SslProtocols specified, uint expectedProtocols)
        {
            var handler = new WinHttpHandler();

            SendRequestHelper.Send(
                handler,
                delegate { handler.SslProtocols = specified; });

            Assert.Equal(expectedProtocols, APICallHistory.WinHttpOptionSecureProtocols);
        }

        [Fact]
        public async Task GetAsync_MultipleRequestsReusingSameClient_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                for (int i = 0; i < 3; i++)
                {
                    TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);
                    using (HttpResponseMessage response = await client.GetAsync(TestServer.FakeServerEndpoint))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task SendAsync_ReadFromStreamingServer_PartialDataRead()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);
                TestServer.DataAvailablePercentage = 0.25;

                int bytesRead;
                byte[] buffer = new byte[TestServer.ExpectedResponseBody.Length];
                var request = new HttpRequestMessage(HttpMethod.Get, TestServer.FakeServerEndpoint);
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var stream = await response.Content.ReadAsStreamAsync();
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    _output.WriteLine("bytesRead={0}", bytesRead);
                }
                Assert.True(bytesRead < buffer.Length, "bytesRead should be less than buffer.Length");
            }
        }

        [Fact]
        public async Task SendAsync_ReadAllDataFromStreamingServer_AllDataRead()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);
                TestServer.DataAvailablePercentage = 0.25;

                int totalBytesRead = 0;
                int bytesRead;
                byte[] buffer = new byte[TestServer.ExpectedResponseBody.Length];
                var request = new HttpRequestMessage(HttpMethod.Get, TestServer.FakeServerEndpoint);
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var stream = await response.Content.ReadAsStreamAsync();
                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        _output.WriteLine("bytesRead={0}", bytesRead);
                        totalBytesRead += bytesRead;
                    } while (bytesRead != 0);
                }
                Assert.Equal(buffer.Length, totalBytesRead);
            }
        }

        [Fact]
        public async Task SendAsync_PostContentWithContentLengthAndChunkedEncodingHeaders_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = true;
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

                var content = new StringContent(TestServer.ExpectedResponseBody);
                Assert.True(content.Headers.ContentLength.HasValue);
                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);
                request.Content = content;

                (await client.SendAsync(request)).Dispose();
            }
        }

        [Fact]
        public async Task SendAsync_PostNoContentObjectWithChunkedEncodingHeader_ExpectHttpRequestException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = true;
                TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
            }
        }

        [Fact]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsDeflateCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    TestServer.SetResponse(DecompressionMethods.Deflate, TestServer.ExpectedResponseBody);
                }))
            {
                await VerifyResponseContent(
                    TestServer.ExpectedResponseBodyBytes,
                    response.Content,
                    responseContentWasOriginallyCompressed: true,
                    responseContentWasAutoDecompressed: true);
            }
        }

        [Fact]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsGZipCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    TestServer.SetResponse(DecompressionMethods.GZip, TestServer.ExpectedResponseBody);
                }))
            {
                await VerifyResponseContent(
                    TestServer.ExpectedResponseBodyBytes,
                    response.Content,
                    responseContentWasOriginallyCompressed: true,
                    responseContentWasAutoDecompressed: true);
            }
        }

        [Fact]
        public async Task SendAsync_NoWinHttpDecompressionSupportAndResponseBodyIsNotCompressed_ExpectedResponse()
        {
            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                }))
            {
                await VerifyResponseContent(
                    TestServer.ExpectedResponseBodyBytes,
                    response.Content,
                    responseContentWasOriginallyCompressed: false,
                    responseContentWasAutoDecompressed: false);

            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_NoWinHttpDecompressionSupport_AutoDecompressionSettingDiffers_ResponseIsNotDecompressed(bool responseIsGZip)
        {
            DecompressionMethods decompressionMethods = responseIsGZip ? DecompressionMethods.Deflate : DecompressionMethods.GZip;
            _output.WriteLine("DecompressionMethods = {0}", decompressionMethods.ToString());

            TestControl.WinHttpDecompressionSupport = false;
            var handler = new WinHttpHandler();

            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.AutomaticDecompression = decompressionMethods;
                    TestServer.SetResponse(responseIsGZip ? DecompressionMethods.GZip : DecompressionMethods.Deflate, TestServer.ExpectedResponseBody);
                }))
            {
                await VerifyResponseContent(
                    TestServer.CompressBytes(TestServer.ExpectedResponseBodyBytes, useGZip: responseIsGZip),
                    response.Content,
                    responseContentWasOriginallyCompressed: true,
                    responseContentWasAutoDecompressed: false);

            }
        }

        [Fact]
        public void SendAsync_AutomaticProxySupportAndUseWinInetSettings_ExpectedWinHttpSessionProxySettings()
        {
            TestControl.WinHttpAutomaticProxySupport = true;
            var handler = new WinHttpHandler();

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
            Assert.Null(APICallHistory.RequestProxySettings.AccessType);
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(handler, delegate { handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy; });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY, APICallHistory.SessionProxySettings.AccessType);
        }

        [Fact]
        public void SendAsync_UseCustomProxyWithNoBypass_ExpectedWinHttpProxySettings()
        {
            var handler = new WinHttpHandler();
            var customProxy = new CustomProxy(false);

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
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

            SendRequestHelper.Send(
                handler,
                delegate
                {
                    handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    handler.Proxy = new FakeDefaultWebProxy();
                });

            Assert.Equal(Interop.WinHttp.WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, APICallHistory.SessionProxySettings.AccessType);
        }

        [Fact]
        public async Task SendAsync_SlowPostRequestWithTimedCancellation_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            TestControl.WinHttpReceiveResponse.Delay = 5000;
            CancellationTokenSource cts = new CancellationTokenSource(50);
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, TestServer.FakeServerEndpoint);
                var content = new StringContent(new string('a', 1000));
                request.Content = content;

                await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
            }
        }

        [Fact]
        public async Task SendAsync_SlowGetRequestWithTimedCancellation_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            TestControl.WinHttpReceiveResponse.Delay = 5000;
            CancellationTokenSource cts = new CancellationTokenSource(50);
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
            }
        }

        [Fact]
        public async Task SendAsync_RequestWithCanceledToken_ExpectTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, TestServer.FakeServerEndpoint);

                await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
            }
        }

        [Fact]
        public async Task SendAsync_WinHttpOpenReturnsError_ExpectHttpRequestException()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, TestServer.FakeServerEndpoint);

            TestControl.WinHttpOpen.ErrorWithApiCall = true;

            Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
            Assert.Equal(typeof(WinHttpException), ex.InnerException.GetType());
        }
        
        [Fact]
        public void SendAsync_MultipleCallsWithDispose_NoHandleLeaksManuallyVerifiedUsingLogging()
        {
            for (int i = 0; i < 50; i++)
            {
                using (var handler = new WinHttpHandler())
                using (HttpResponseMessage response = SendRequestHelper.Send(handler, () => { }))
                {
                }
            }
        }

        private async Task VerifyResponseContent(
            byte[] expectedResponseBodyBytes,
            HttpContent responseContent,
            bool responseContentWasOriginallyCompressed,
            bool responseContentWasAutoDecompressed)
        {
            Nullable<long> contentLength = responseContent.Headers.ContentLength;
            ICollection<string> contentEncoding = responseContent.Headers.ContentEncoding;

            _output.WriteLine("Response Content.Headers.ContentLength = {0}", contentLength.HasValue ? contentLength.Value.ToString() : "(null)");
            _output.WriteLine("Response Content.Headers.ContentEncoding = {0}", contentEncoding.Count > 0 ? contentEncoding.ToString() : "(null)");
            byte[] responseBodyBytes = await responseContent.ReadAsByteArrayAsync();
            _output.WriteLine($"Response Body          = {BitConverter.ToString(responseBodyBytes)}");
            _output.WriteLine($"Expected Response Body = {BitConverter.ToString(expectedResponseBodyBytes)}");

            if (!responseContentWasOriginallyCompressed)
            {
                Assert.True(contentLength > 0);
            }
            else if (responseContentWasAutoDecompressed)
            {
                
                Assert.Null(contentLength);
                Assert.Equal(0, contentEncoding.Count);
            }
            else
            {
                Assert.True(contentLength > 0);
                Assert.True(contentEncoding.Count > 0);
            }

            Assert.Equal<byte>(expectedResponseBodyBytes, responseBodyBytes);
        }

        // Commented out as the test relies on finalizer for cleanup and only has value as written
        // when run on its own and manual analysis is done of logs.
        //[Fact]
        //public void SendAsync_MultipleCallsWithoutDispose_NoHandleLeaksManuallyVerifiedUsingLogging()
        //{
        //    WinHttpHandler handler;
        //    HttpResponseMessage response;
        //    for (int i = 0; i < 50; i++)
        //    {
        //        handler = new WinHttpHandler();
        //        response = SendRequestHelper.Send(handler, () => { });
        //    }
        //}

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
