// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal sealed class CertificateValidationDelegateWrapper : IDisposable
    {
        private static uint s_initialize = 1;
        private readonly RemoteCertificateValidationCallback _callback;
        private readonly GCHandle _handle;

        public IntPtr Pointer => GCHandle.ToIntPtr(_handle);

        public CertificateValidationDelegateWrapper(
            RemoteCertificateValidationCallback callback)
        {
            if (Interlocked.CompareExchange(ref s_initialize, 0, 1) == 1)
            {
                Init();
            }

            _callback = callback;
            _handle = GCHandle.Alloc(this);
        }

        public void Dispose()
        {
            _handle.Free();
        }

        private static unsafe void Init()
        {
            Interop.AndroidCrypto.RegisterTrustManagerValidationCallbackImpl(&ValidateCallback);
        }

        [UnmanagedCallersOnly]
        private static unsafe bool ValidateCallback(
            IntPtr validatorHandle,
            byte** rawCertificates,
            int* certificateLengths,
            int certificatesCount,
            int errors)
        {
            CertificateValidationDelegateWrapper? validator = (CertificateValidationDelegateWrapper?)GCHandle.FromIntPtr(validatorHandle).Target;
            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validatorHandle));
            }

            var certificates = new X509Certificate2[certificatesCount];
            for (int i = 0; i < certificatesCount; i++)
            {
                var rawData = new ReadOnlySpan<byte>(rawCertificates[i], certificateLengths[i]);
                certificates[i] = new X509Certificate2(rawData);
            }

            return validator.Validate(certificates, (SslPolicyErrors)errors);
        }

        private bool Validate(X509Certificate2[] certificates, SslPolicyErrors errors)
        {
            // TODO
            object sender = null!;
            X509Certificate2? certificate = certificates.Length > 0 ? certificates[0] : null;
            X509Chain chain = null!;
            return _callback.Invoke(sender, certificate, chain, errors);
        }
    }
}
