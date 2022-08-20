// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Chain : IDisposable
    {
        private X509ChainPolicy? _chainPolicy;
        private volatile X509ChainStatus[]? _lazyChainStatus;
        private X509ChainElementCollection? _chainElements;
        private IChainPal? _pal;
        private bool _useMachineContext;
        private readonly object _syncRoot = new object();

        public X509Chain() { }

        public X509Chain(bool useMachineContext)
        {
            _useMachineContext = useMachineContext;
        }

        [SupportedOSPlatform("windows")]
        public X509Chain(IntPtr chainContext)
        {
            _pal = ChainPal.FromHandle(chainContext);
            Debug.Assert(_pal != null);
            _chainElements = new X509ChainElementCollection(_pal.ChainElements!);
        }

        public static X509Chain Create()
        {
            return new X509Chain();
        }

        public X509ChainElementCollection ChainElements => _chainElements ??= new X509ChainElementCollection();

        public X509ChainPolicy ChainPolicy
        {
            get => _chainPolicy ??= new X509ChainPolicy();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _chainPolicy = value;
            }
        }

        public X509ChainStatus[] ChainStatus
        {
            get
            {
                // We give the user a reference to the array since we'll never access it.
                return _lazyChainStatus ??= (_pal == null ? Array.Empty<X509ChainStatus>() : _pal.ChainStatus!);
            }
        }

        public IntPtr ChainContext
        {
            get
            {
                SafeX509ChainHandle? handle = SafeHandle;
                if (handle == null)
                {
                    // This case will only exist for Unix
                    return IntPtr.Zero;
                }

                // For .NET Framework compat, we may return an invalid handle here (IntPtr.Zero)
                return handle.DangerousGetHandle();
            }
        }

        public SafeX509ChainHandle? SafeHandle
        {
            get
            {
                if (_pal == null)
                    return SafeX509ChainHandle.InvalidHandle;

                return _pal.SafeHandle;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public bool Build(X509Certificate2 certificate)
        {
            return Build(certificate, true);
        }

        internal bool Build(X509Certificate2 certificate, bool throwOnException)
        {
            lock (_syncRoot)
            {
                if (certificate == null || certificate.Pal == null)
                    throw new ArgumentException(SR.Cryptography_InvalidContextHandle, nameof(certificate));

                if (_chainPolicy != null)
                {
                    if (_chainPolicy._customTrustStore != null)
                    {
                        if (_chainPolicy.TrustMode == X509ChainTrustMode.System && _chainPolicy.CustomTrustStore.Count > 0)
                            throw new CryptographicException(SR.Cryptography_CustomTrustCertsInSystemMode);

                        foreach (X509Certificate2 customCertificate in _chainPolicy.CustomTrustStore)
                        {
                            if (customCertificate == null || customCertificate.Handle == IntPtr.Zero)
                            {
                                throw new CryptographicException(SR.Cryptography_InvalidTrustCertificate);
                            }
                        }
                    }

                    if (_chainPolicy.TrustMode == X509ChainTrustMode.CustomRootTrust && _chainPolicy._customTrustStore == null)
                    {
                        _chainPolicy._customTrustStore = new X509Certificate2Collection();
                    }
                }

                Reset();

                X509ChainPolicy chainPolicy = ChainPolicy;
                _pal = ChainPal.BuildChain(
                    _useMachineContext,
                    certificate.Pal,
                    chainPolicy._extraStore,
                    chainPolicy._applicationPolicy!,
                    chainPolicy._certificatePolicy!,
                    chainPolicy.RevocationMode,
                    chainPolicy.RevocationFlag,
                    chainPolicy._customTrustStore,
                    chainPolicy.TrustMode,
                    chainPolicy.VerificationTimeIgnored ? DateTime.Now : chainPolicy.VerificationTime,
                    chainPolicy.UrlRetrievalTimeout,
                    chainPolicy.DisableCertificateDownloads);

                if (_pal == null)
                    return false;

                _chainElements = new X509ChainElementCollection(_pal.ChainElements!);

                Exception? verificationException;
                bool? verified = _pal.Verify(chainPolicy.VerificationFlags, out verificationException);
                if (!verified.HasValue)
                {
                    if (throwOnException)
                    {
                        throw verificationException!;
                    }
                    else
                    {
                        verified = false;
                    }
                }

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

        public void Reset()
        {
            // _chainPolicy is not reset for .NET Framework compat
            _lazyChainStatus = null;
            _chainElements = null;
            _useMachineContext = false;

            IChainPal? pal = _pal;
            if (pal != null)
            {
                _pal = null;
                pal.Dispose();
            }
        }
    }
}
