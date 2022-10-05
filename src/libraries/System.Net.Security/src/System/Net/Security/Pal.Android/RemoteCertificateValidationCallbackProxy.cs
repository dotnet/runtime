// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal sealed class RemoteCertificateValidationCallbackProxy : IDisposable
    {
        private static object s_initializationLock = new();
        private static bool s_initialized;

        private readonly RemoteCertificateValidationCallback _callback;
        private readonly object _sender;
        private readonly GCHandle _handle;

        public IntPtr Handle => GCHandle.ToIntPtr(_handle);

        public unsafe RemoteCertificateValidationCallbackProxy(
            object sender,
            RemoteCertificateValidationCallback callback)
        {
            EnsureTrustManagerValidationCallbackIsRegistered();

            _sender = sender;
            _callback = callback;
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
            bool approvedByDefaultTrustManager)
        {
            RemoteCertificateValidationCallbackProxy validator = FromHandle(validatorHandle);
            X509Certificate2[] certificates = Convert(certificatesCount, certificateLengths, rawCertificates);

            return validator.Validate(certificates, approvedByDefaultTrustManager);
        }

        private bool Validate(X509Certificate2[] certificates, bool approvedByDefaultTrustManager)
        {
            var errors = approvedByDefaultTrustManager
                ? SslPolicyErrors.None
                : certificates.Length == 0
                    ? SslPolicyErrors.RemoteCertificateNotAvailable
                    : SslPolicyErrors.RemoteCertificateChainErrors;

            X509Certificate2? certificate = certificates.Length > 0 ? certificates[0] : null;
            X509Chain chain = CreateChain(certificates);
            return _callback.Invoke(_sender, certificate, chain, errors);
        }

        private static RemoteCertificateValidationCallbackProxy FromHandle(IntPtr handle)
            => GCHandle.FromIntPtr(handle).Target as RemoteCertificateValidationCallbackProxy
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

        private static X509Chain CreateChain (X509Certificate2[] certificates)
        {
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.ExtraStore.AddRange(certificates);
            return chain;
        }
    }
}
