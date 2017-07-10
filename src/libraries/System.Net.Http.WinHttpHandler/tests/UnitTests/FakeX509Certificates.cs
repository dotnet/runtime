// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http.WinHttpHandlerUnitTests;
using System.Security.Cryptography.X509Certificates;

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
