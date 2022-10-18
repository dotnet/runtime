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
        private GCHandle? _handle;

        public unsafe TrustManagerProxy(RemoteCertificateVerification remoteCertificateVerifier)
        {
            EnsureTrustManagerValidationCallbackIsRegistered();

            _remoteCertificateVerifier = remoteCertificateVerifier;
            _handle = GCHandle.Alloc(this);
        }

        public IntPtr Handle
            => _handle is GCHandle handle
                ? GCHandle.ToIntPtr(handle)
                : throw new ObjectDisposedException(nameof(TrustManagerProxy));

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
            IntPtr proxyPtr,
            int certificatesCount,
            int* certificateLengths,
            byte** rawCertificates)
        {
            var proxy = (TrustManagerProxy?)GCHandle.FromIntPtr(proxyPtr).Target;
            Debug.Assert(proxy is not null);

            X509Certificate2[] certificates = ConvertCertificates(certificatesCount, certificateLengths, rawCertificates);
            try
            {
                return proxy.Validate(certificates);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Remote certificate verification has thrown an exception: {exception}");
                Debug.WriteLine(exception.StackTrace);
                return false;
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

        private static unsafe X509Certificate2[] ConvertCertificates(int count, int* lengths, byte** rawData)
        {
            var certificates = new X509Certificate2[count];

            for (int i = 0; i < count; i++)
            {
                var rawCertificate = new ReadOnlySpan<byte>(rawData[i], lengths[i]);
                certificates[i] = new X509Certificate2(rawCertificate);
            }

            return certificates;
        }
    }
}
