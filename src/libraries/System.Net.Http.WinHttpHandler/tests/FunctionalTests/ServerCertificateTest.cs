// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WinHttpHandler not supported on UAP")]
    public class ServerCertificateTest
    {
        private readonly ITestOutputHelper _output;
        private readonly ValidationCallbackHistory _validationCallbackHistory;

        public ServerCertificateTest(ITestOutputHelper output)
        {
            _output = output;
            _validationCallbackHistory = new ValidationCallbackHistory();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task NoCallback_ValidCertificate_CallbackNotCalled()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(_validationCallbackHistory.WasCalled);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(_validationCallbackHistory.WasCalled);
            }
        }

        [OuterLoop] // TODO: Issue #11345
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
                
                ConfirmValidCertificate(System.Net.Test.Common.Configuration.Http.Host);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_RedirectandValidCertificate_ExpectedValuesDuringCallback()
        {
            Uri uri = System.Net.Test.Common.Configuration.Http.RedirectUriForDestinationUri(true, 302, System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer, 1);

            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(_validationCallbackHistory.WasCalled);
                
                ConfirmValidCertificate(System.Net.Test.Common.Configuration.Http.Host);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsInnerSecurityFailureException()
        {
            const int ERROR_WINHTTP_SECURE_FAILURE = 12175;

            var handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer);
                _validationCallbackHistory.ReturnFailure = true;
                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    client.GetAsync(System.Net.Test.Common.Configuration.Http.SecureRemoteEchoServer));
                var innerEx = (Win32Exception)ex.InnerException;
                Assert.Equal(ERROR_WINHTTP_SECURE_FAILURE, innerEx.NativeErrorCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
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

        private void ConfirmValidCertificate(string expectedHostName)
        {
                Assert.Equal(SslPolicyErrors.None, _validationCallbackHistory.SslPolicyErrors);
                Assert.True(_validationCallbackHistory.CertificateChain.Count > 0);
                _output.WriteLine("Certificate.Subject: {0}", _validationCallbackHistory.CertificateSubject);
                _output.WriteLine("Expected HostName: {0}", expectedHostName);
        }
        
        private bool CustomServerCertificateValidationCallback(
            HttpRequestMessage sender,
            X509Certificate2 certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            _validationCallbackHistory.WasCalled = true;
            _validationCallbackHistory.CertificateSubject = certificate.Subject;
            foreach (var element in chain.ChainElements)
            {
                _validationCallbackHistory.CertificateChain.Add(element.Certificate);
            }
            _validationCallbackHistory.ChainStatus = chain.ChainStatus;
            _validationCallbackHistory.SslPolicyErrors = sslPolicyErrors;

            if (_validationCallbackHistory.ThrowException)
            {
                throw new CustomException();
            }

            if (_validationCallbackHistory.ReturnFailure)
            {
                return false;
            }

            return true;
        }

        public class CustomException : Exception
        {
            public CustomException()
            {
            }
        }
        
        public class ValidationCallbackHistory
        {
            public bool ThrowException;
            public bool ReturnFailure;
            public bool WasCalled;
            public SslPolicyErrors SslPolicyErrors;
            public string CertificateSubject;
            public X509CertificateCollection CertificateChain;
            public X509ChainStatus[] ChainStatus;

            public ValidationCallbackHistory()
            {
                ThrowException = false;
                ReturnFailure = false;
                WasCalled = false;
                SslPolicyErrors = SslPolicyErrors.None;
                CertificateSubject = null;
                CertificateChain = new X509CertificateCollection();
                ChainStatus = null;
            }
        }
    }
}
