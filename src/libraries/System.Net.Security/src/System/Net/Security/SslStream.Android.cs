// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private JavaProxy.RemoteCertificateValidationResult VerifyRemoteCertificate()
        {
            try
            {
                ProtocolToken? alertToken = null;
                var isValid = VerifyRemoteCertificate(
                    _sslAuthenticationOptions.CertValidationDelegate,
                    _sslAuthenticationOptions.CertificateContext?.Trust,
                    ref alertToken,
                    out SslPolicyErrors sslPolicyErrors,
                    out X509ChainStatusFlags chainStatus);

                return new JavaProxy.RemoteCertificateValidationResult
                {
                    IsValid = isValid,
                    SslPolicyErrors = sslPolicyErrors,
                    ChainStatus = chainStatus,
                };
            }
            catch (Exception exception)
            {
                return new JavaProxy.RemoteCertificateValidationResult
                {
                    IsValid = false,
                    CaughtException = exception,
                    SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors,
                    ChainStatus = X509ChainStatusFlags.NoError,
                };
            }
        }

        internal sealed class JavaProxy : IDisposable
        {
            private static object s_initializationLock = new();
            private static bool s_initialized;

            private readonly SslStream _sslStream;
            private GCHandle? _handle;

            public unsafe JavaProxy(SslStream sslStream)
            {
                RegisterRemoteCertificateValidationCallback();

                _sslStream = sslStream;
                _handle = GCHandle.Alloc(this);
            }

            public IntPtr Handle
                => _handle is GCHandle handle
                    ? GCHandle.ToIntPtr(handle)
                    : throw new ObjectDisposedException(nameof(JavaProxy));

            public RemoteCertificateValidationResult? ValidationResult { get; private set; }

            private static unsafe void RegisterRemoteCertificateValidationCallback()
            {
                lock (s_initializationLock)
                {
                    if (!s_initialized)
                    {
                        Interop.AndroidCrypto.RegisterRemoteCertificateValidationCallback(&VerifyRemoteCertificate);
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
            private static unsafe bool VerifyRemoteCertificate(IntPtr sslStreamProxyHandle)
            {
                var proxy = (JavaProxy?)GCHandle.FromIntPtr(sslStreamProxyHandle).Target;
                Debug.Assert(proxy is not null);
                Debug.Assert(proxy.ValidationResult is null);

                proxy.ValidationResult = proxy._sslStream.VerifyRemoteCertificate();
                return proxy.ValidationResult.IsValid;
            }

            internal sealed class RemoteCertificateValidationResult
            {
                public bool IsValid { get; init; }
                public Exception? CaughtException { get; init; }
                public SslPolicyErrors SslPolicyErrors { get; init; }
                public X509ChainStatusFlags ChainStatus { get; init; }
            }
        }
    }
}
