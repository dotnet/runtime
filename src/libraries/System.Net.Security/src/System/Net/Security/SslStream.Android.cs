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
            ProtocolToken? alertToken = null;
            var isValid = VerifyRemoteCertificate(
                _sslAuthenticationOptions.CertValidationDelegate,
                _sslAuthenticationOptions.CertificateContext?.Trust,
                ref alertToken,
                out SslPolicyErrors sslPolicyErrors,
                out X509ChainStatusFlags chainStatus);

            return new()
            {
                IsValid = isValid,
                SslPolicyErrors = sslPolicyErrors,
                ChainStatus = chainStatus,
            };
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

            public Exception? ValidationException { get; private set; }
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

                try
                {
                    proxy.ValidationResult = proxy._sslStream.VerifyRemoteCertificate();
                    return proxy.ValidationResult.IsValid;
                }
                catch (Exception exception)
                {
                    proxy.ValidationException = exception;
                    return false;
                }
            }

            internal sealed class RemoteCertificateValidationResult
            {
                public bool IsValid { get; init; }
                public SslPolicyErrors SslPolicyErrors { get; init; }
                public X509ChainStatusFlags ChainStatus { get; init; }
            }
        }
    }
}
