// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public class ServerCertificateTest : BaseCertificateTest
    {
        public ServerCertificateTest(ITestOutputHelper output) : base(output)
        { }

        public static bool DowngradeToHTTP1IfClientCertSet => PlatformDetection.WindowsVersion < 2004;

        [OuterLoop]
        [Fact]
        public async Task NoCallback_ValidCertificate_CallbackNotCalled()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Test.Common.Configuration.Http.SecureRemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(_validationCallbackHistory.WasCalled);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Test.Common.Configuration.Http.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(_validationCallbackHistory.WasCalled);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task UseCallback_ValidCertificate_ExpectedValuesDuringCallback()
        {
            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(_validationCallbackHistory.WasCalled);

                ConfirmValidCertificate(Test.Common.Configuration.Http.Host);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task UseCallback_RedirectandValidCertificate_ExpectedValuesDuringCallback()
        {
            Uri uri = Test.Common.Configuration.Http.RemoteSecureHttp11Server.RedirectUriForDestinationUri(302, System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer, 1);

            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(_validationCallbackHistory.WasCalled);

                ConfirmValidCertificate(Test.Common.Configuration.Http.Host);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsInnerSecurityFailureException()
        {
            const int ERROR_WINHTTP_SECURE_FAILURE = 12175;

            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, Test.Common.Configuration.Http.SecureRemoteEchoServer);
                _validationCallbackHistory.ReturnFailure = true;
                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    client.GetAsync(System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer));
                var innerEx = (Win32Exception)ex.InnerException;
                Assert.Equal(ERROR_WINHTTP_SECURE_FAILURE, innerEx.NativeErrorCode);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task UseCallback_CallbackThrowsSpecificException_SpecificExceptionPropagatesAsBaseException()
        {
            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            {
                _validationCallbackHistory.ThrowException = true;
                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    client.GetAsync(System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer));
                Assert.True(ex.GetBaseException() is CustomException);
            }
        }
    }
}
