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

        private readonly GCHandle _handle;
        private readonly RemoteCertificateVerification _remoteCertificateVerifier;


        public IntPtr Handle => GCHandle.ToIntPtr(_handle);

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
                    Interop.AndroidCrypto.RegisterTrustManagerValidationCallbackImpl(&TrustManagerCallback);
                    s_initialized = true;
                }
            }
        }

        public void Dispose()
            => _handle.Free();


        [UnmanagedCallersOnly]
        private static unsafe bool TrustManagerCallback(
            IntPtr validatorHandle,
            int certificatesCount,
            int* certificateLengths,
            byte** rawCertificates,
            int errors)
        {
            TrustManagerProxy validator = FromHandle(validatorHandle);
            X509Certificate2[] certificates = Convert(certificatesCount, certificateLengths, rawCertificates);

            return validator.Validate(certificates, (SslPolicyErrors)errors);
        }

        private bool Validate(X509Certificate2[] certificates, SslPolicyErrors errors)
        {
            X509Certificate2? certificate = certificates.Length > 0 ? certificates[0] : null;

            // TODO what to do with the rest of the certificates?
            // should I create an instance of SslCertificateTrust?

            return _remoteCertificateVerifier.VerifyRemoteCertificate(
                certificate,
                trust: null,
                chain: null,
                remoteCertRequired: true,
                ref errors,
                out _);
        }

        private static TrustManagerProxy FromHandle(IntPtr handle)
            => GCHandle.FromIntPtr(handle).Target as TrustManagerProxy
                ?? throw new ArgumentNullException(nameof(handle));

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
