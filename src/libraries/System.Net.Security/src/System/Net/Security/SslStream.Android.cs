// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private JavaProxy.RemoteCertificateValidationResult VerifyRemoteCertificate(bool chainTrustedByPlatform)
        {
            // If the platform's trust manager rejected the certificate chain,
            // report RemoteCertificateChainErrors so the callback can handle it.
            SslPolicyErrors sslPolicyErrors = chainTrustedByPlatform
                ? SslPolicyErrors.None
                : SslPolicyErrors.RemoteCertificateChainErrors;

            ProtocolToken alertToken = default;

            RemoteCertificateValidationCallback? userCallback = _sslAuthenticationOptions.CertValidationDelegate;
            RemoteCertificateValidationCallback? effectiveCallback = userCallback;

            if (chainTrustedByPlatform)
            {
                // The platform's trust manager (which respects network-security-config.xml)
                // already validated the certificate chain. The managed X509 chain builder may
                // report RemoteCertificateChainErrors because the root CA trusted by the platform
                // is not in the managed certificate store (e.g. PartialChain or UntrustedRoot).
                // Strip chain errors — the platform's assessment is authoritative for chain trust.
                //
                // We wrap (or provide) the callback so that:
                // 1. User callbacks see errors without spurious RemoteCertificateChainErrors.
                // 2. When no user callback is set, the default "accept if no errors" logic
                //    doesn't reject connections that the platform already accepted.
                effectiveCallback = userCallback is not null
                    ? (sender, certificate, chain, errors) =>
                        userCallback(sender, certificate, chain, errors & ~SslPolicyErrors.RemoteCertificateChainErrors)
                    : (sender, certificate, chain, errors) =>
                        (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;
            }

            var isValid = VerifyRemoteCertificate(
                effectiveCallback,
                _sslAuthenticationOptions.CertificateContext?.Trust,
                ref alertToken,
                ref sslPolicyErrors,
                out X509ChainStatusFlags chainStatus);

            if (chainTrustedByPlatform)
            {
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return new()
            {
                IsValid = isValid,
                SslPolicyErrors = sslPolicyErrors,
                ChainStatus = chainStatus,
                AlertToken = alertToken,
            };
        }

        private bool TryGetRemoteCertificateValidationResult(out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus, ref ProtocolToken alertToken, out bool isValid)
        {
            JavaProxy.RemoteCertificateValidationResult? validationResult = _securityContext?.SslStreamProxy.ValidationResult;
            sslPolicyErrors = validationResult?.SslPolicyErrors ?? default;
            chainStatus = validationResult?.ChainStatus ?? default;
            isValid = validationResult?.IsValid ?? default;
            alertToken = validationResult?.AlertToken ?? default;
            return validationResult is not null;
        }

        internal sealed class JavaProxy : IDisposable
        {
            private static bool s_initialized;

            private readonly SslStream _sslStream;
            private GCHandle? _handle;

            public IntPtr Handle
                => _handle is GCHandle handle
                    ? GCHandle.ToIntPtr(handle)
                    : throw new ObjectDisposedException(nameof(JavaProxy));

            public Exception? ValidationException { get; private set; }
            public RemoteCertificateValidationResult? ValidationResult { get; private set; }

            public JavaProxy(SslStream sslStream)
            {
                RegisterRemoteCertificateValidationCallback();

                _sslStream = sslStream;
                _handle = GCHandle.Alloc(this);
            }

            public void Dispose()
            {
                _handle?.Free();
                _handle = null;
            }

            private static unsafe void RegisterRemoteCertificateValidationCallback()
            {
                if (!s_initialized)
                {
                    Interop.AndroidCrypto.RegisterRemoteCertificateValidationCallback(&VerifyRemoteCertificate);
                    s_initialized = true;
                }
            }

            [UnmanagedCallersOnly]
            private static unsafe bool VerifyRemoteCertificate(IntPtr sslStreamProxyHandle, int chainTrustedByPlatform)
            {
                var proxy = (JavaProxy?)GCHandle.FromIntPtr(sslStreamProxyHandle).Target;
                Debug.Assert(proxy is not null);
                Debug.Assert(proxy.ValidationResult is null);

                try
                {
                    proxy.ValidationResult = proxy._sslStream.VerifyRemoteCertificate(chainTrustedByPlatform != 0);
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
                public ProtocolToken? AlertToken { get; init; }
            }
        }
    }
}
