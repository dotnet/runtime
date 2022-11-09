// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal sealed class TrustManagerProxy : IDisposable
    {
        private static object s_initializationLock = new();
        private static bool s_initialized;

        private readonly RemoteCertificateVerification _remoteCertificateVerifier;
        private readonly SafeDeleteSslContext _securityContext;
        private GCHandle? _handle;

        public unsafe TrustManagerProxy(
            RemoteCertificateVerification remoteCertificateVerifier,
            SafeDeleteSslContext securityContext)
        {
            RegisterTrustManagerCallback();

            _remoteCertificateVerifier = remoteCertificateVerifier;
            _securityContext = securityContext;
            _handle = GCHandle.Alloc(this);
        }

        public IntPtr Handle
            => _handle is GCHandle handle
                ? GCHandle.ToIntPtr(handle)
                : throw new ObjectDisposedException(nameof(TrustManagerProxy));

        public Exception? CaughtException { get; private set; }

        private static unsafe void RegisterTrustManagerCallback()
        {
            lock (s_initializationLock)
            {
                if (!s_initialized)
                {
                    Interop.AndroidCrypto.RegisterTrustManagerCallback(&VerifyRemoteCertificate);
                    s_initialized = true;
                }
            }
        }

        public void Dispose()
        {
            _handle?.Free();
            _handle = null;
        }

        [UnmanagedCallersOnly]
        private static unsafe bool VerifyRemoteCertificate(
            IntPtr trustManagerProxyHandle,
            int certificateCount,
            IntPtr* certificatePtrs)
        {
            var proxy = (TrustManagerProxy?)GCHandle.FromIntPtr(trustManagerProxyHandle).Target;
            Debug.Assert(proxy is not null);

            X509Certificate2[] certificates = ConvertCertificates(certificateCount, certificatePtrs);
            try
            {
                return proxy.Verify(certificates);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Remote certificate verification has thrown an exception: {exception}");
                Debug.WriteLine(exception.StackTrace);

                proxy.CaughtException = exception;
                return false;
            }
            finally
            {
                foreach (var certificate in certificates)
                    certificate.Dispose();
            }
        }

        private bool Verify(X509Certificate2[] certificates)
        {
            X509Certificate2? certificate = certificates.Length > 0 ? certificates[0] : null;
            X509Chain? chain = null;
            if (certificates.Length > 1)
            {
                chain = new X509Chain();
                chain.ChainPolicy.ExtraStore.AddRange(certificates[1..]);
            }

            try
            {
                return _remoteCertificateVerifier.Verify(certificate, _securityContext, trust: null, ref chain, out _, out _);
            }
            finally
            {
                if (chain != null)
                {
                    int elementsCount = chain.ChainElements.Count;
                    for (int i = 0; i < elementsCount; i++)
                    {
                        chain.ChainElements[i].Certificate.Dispose();
                    }

                    chain.Dispose();
                }
            }
        }

        private static unsafe X509Certificate2[] ConvertCertificates(int count, IntPtr* certificatePtrs)
        {
            var certificates = new X509Certificate2[count];
            for (int i = 0; i < count; i++)
            {
                using var handle = new SafeX509Handle(certificatePtrs[i]);
                certificates[i] = new X509Certificate2(handle.DangerousGetHandle());
            }

            return certificates;
        }
    }
}
