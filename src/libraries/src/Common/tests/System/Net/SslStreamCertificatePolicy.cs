// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCertificatePolicy : IDisposable
    {
        private SslPolicyErrors _ignoredPolicyErrors;

        public SslStreamCertificatePolicy(SslPolicyErrors ignoredPolicyErrors = (SslPolicyErrors)0xFFFF)
        {
            _ignoredPolicyErrors = ignoredPolicyErrors;
        }

        public bool SslStreamCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if ((sslPolicyErrors | _ignoredPolicyErrors) == _ignoredPolicyErrors)
            {
                return true;
            }

            return false;
        }

        private bool VerifyPolicy(SslPolicyErrors sslPolicyErrors)
        {
            if ((sslPolicyErrors | _ignoredPolicyErrors) == _ignoredPolicyErrors)
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}
