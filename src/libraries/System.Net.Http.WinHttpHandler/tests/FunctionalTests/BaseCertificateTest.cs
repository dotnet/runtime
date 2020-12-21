// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public abstract class BaseCertificateTest
    {
        private readonly ITestOutputHelper _output;

        protected readonly ValidationCallbackHistory _validationCallbackHistory;

        public BaseCertificateTest(ITestOutputHelper output)
        {
            _output = output;
            _validationCallbackHistory = new ValidationCallbackHistory();
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

        protected bool CustomServerCertificateValidationCallback(
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

        protected void ConfirmValidCertificate(string expectedHostName)
        {
            Assert.Equal(SslPolicyErrors.None, _validationCallbackHistory.SslPolicyErrors);
            Assert.True(_validationCallbackHistory.CertificateChain.Count > 0);
            _output.WriteLine("Certificate.Subject: {0}", _validationCallbackHistory.CertificateSubject);
            _output.WriteLine("Expected HostName: {0}", expectedHostName);
        }

        public class CustomException : Exception
        {
            public CustomException()
            {
            }
        }
    }
}
