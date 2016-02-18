// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Chain : IDisposable
    {
        public X509Chain()
        {
            Reset();
        }

        public X509ChainElementCollection ChainElements
        {
            get { return _chainElements; }
        }

        public X509ChainPolicy ChainPolicy
        {
            get
            {
                if (_chainPolicy == null)
                    _chainPolicy = new X509ChainPolicy();
                return _chainPolicy;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _chainPolicy = value;
            }
        }

        public X509ChainStatus[] ChainStatus
        {
            get
            {
                // We give the user a reference to the array since we'll never access it.
                X509ChainStatus[] chainStatus = _lazyChainStatus;
                if (chainStatus == null)
                    chainStatus = _lazyChainStatus = (_pal == null ? Array.Empty<X509ChainStatus>() : _pal.ChainStatus);
                return chainStatus;
            }
        }

        public SafeX509ChainHandle SafeHandle
        {
            get
            {
                if (_pal == null)
                    return SafeX509ChainHandle.InvalidHandle;

                return _pal.SafeHandle;
            }
        }

        public bool Build(X509Certificate2 certificate)
        {
            lock (_syncRoot)
            {
                if (certificate == null)
                    throw new ArgumentException(SR.Cryptography_InvalidContextHandle, nameof(certificate));

                Reset();

                X509ChainPolicy chainPolicy = ChainPolicy;
                _pal = ChainPal.BuildChain(
                    false,
                    certificate.Pal,
                    chainPolicy.ExtraStore,
                    chainPolicy.ApplicationPolicy,
                    chainPolicy.CertificatePolicy,
                    chainPolicy.RevocationMode,
                    chainPolicy.RevocationFlag,
                    chainPolicy.VerificationTime,
                    chainPolicy.UrlRetrievalTimeout
                    );
                if (_pal == null)
                    return false;

                _chainElements = new X509ChainElementCollection(_pal.ChainElements);

                Exception verificationException;
                bool? verified = _pal.Verify(chainPolicy.VerificationFlags, out verificationException);
                if (!verified.HasValue)
                    throw verificationException;
                return verified.Value;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }

        private void Reset()
        {
            _lazyChainStatus = null;
            _chainElements = new X509ChainElementCollection();

            IChainPal pal = _pal;
            _pal = null;
            if (pal != null)
                pal.Dispose();
        }

        private X509ChainPolicy _chainPolicy;
        private volatile X509ChainStatus[] _lazyChainStatus;
        private X509ChainElementCollection _chainElements;
        private IChainPal _pal;
        private readonly object _syncRoot = new object();
    }
}

