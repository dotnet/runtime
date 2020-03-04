// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public abstract class BaseCertificateTest
    {
        protected readonly ValidationCallbackHistory _validationCallbackHistory;

        public BaseCertificateTest()
        {
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

        public class CustomException : Exception
        {
            public CustomException()
            {
            }
        }
    }
}
