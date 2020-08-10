// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.WinHttpHandlerUnitTests;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    internal static class WinHttpCertificateHelper
    {
        public static void BuildChain(
            X509Certificate2 certificate,
            X509Certificate2Collection remoteCertificateStore,
            string hostName,
            bool checkCertificateRevocationList,
            out X509Chain chain,
            out SslPolicyErrors sslPolicyErrors)
        {
            chain = null;
            sslPolicyErrors = SslPolicyErrors.None;
        }
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Store : IDisposable
    {
        private bool _disposed;

        public X509Store(StoreName storeName, StoreLocation storeLocation)
        {
            Debug.Assert(storeName == StoreName.My);
            Debug.Assert(storeLocation == StoreLocation.CurrentUser);
        }

        public X509Certificate2Collection Certificates
        {
            get
            {
                return TestControl.CurrentUserCertificateStore;
            }
        }

        public void Open(OpenFlags flags)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
