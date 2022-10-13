// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        private GCHandle? _handle;

        public IntPtr Handle
            => _handle is GCHandle handle
                ? GCHandle.ToIntPtr(handle)
                : throw new ObjectDisposedException(nameof(TrustManagerProxy));

        public unsafe TrustManagerProxy(RemoteCertificateVerification remoteCertificateVerifier)
        {
            EnsureTrustManagerValidationCallbackIsRegistered();

            _remoteCertificateVerifier = remoteCertificateVerifier;
            _handle = GCHandle.Alloc(this);
        }

        private static unsafe void EnsureTrustManagerValidationCallbackIsRegistered()
        {
            lock (s_initializationLock)
            {
                if (!s_initialized)
                {
                    Interop.AndroidCrypto.RegisterTrustManagerValidationCallback(&TrustManagerCallback);
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
        private static unsafe bool TrustManagerCallback(
            IntPtr proxyHandle,
            int certificatesCount,
            int* certificateLengths,
            byte** rawCertificates)
        {
            TrustManagerProxy proxy = FromHandle(proxyHandle);
            X509Certificate2[] certificates = Convert(certificatesCount, certificateLengths, rawCertificates);

            try
            {
                return proxy.Validate(certificates);
            }
            finally
            {
                foreach (var certificate in certificates)
                    certificate.Dispose();
            }
        }

        private bool Validate(X509Certificate2[] certificates)
        {
            X509Certificate2? certificate = certificates.Length > 0 ? certificates[0] : null;

            X509Chain? chain = null;
            if (certificates.Length > 1)
            {
                chain = new X509Chain();
                chain.ChainPolicy.ExtraStore.AddRange(certificates[1..]);
            }

            return _remoteCertificateVerifier.VerifyRemoteCertificate(certificate, trust: null, chain, out _, out _);
        }

        private static TrustManagerProxy FromHandle(IntPtr handle)
            => GCHandle.FromIntPtr(handle).Target as TrustManagerProxy
                ?? throw new ObjectDisposedException(nameof(TrustManagerProxy));

        private static unsafe X509Certificate2[] Convert(
            int certificatesCount,
            int* certificateLengths,
            byte** rawCertificates)
        {
            var certificates = new X509Certificate2[certificatesCount];

            for (int i = 0; i < certificatesCount; i++)
            {
                var rawData = new ReadOnlySpan<byte>(rawCertificates[i], certificateLengths[i]);
                certificates[i] = new X509Certificate2(rawData);
            }

            return certificates;
        }
    }
}
