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
            // When the platform's trust manager rejected the chain AND the managed
            // chain builder is using system trust (not CustomRootTrust), pre-inject
            // RemoteCertificateChainErrors. This is critical for scenarios like certificate
            // pinning via network-security-config.xml: the platform rejects the pin mismatch,
            // but the managed chain builder (which doesn't know about pins) would accept the
            // chain as valid. Without pre-injection, pinning would be silently bypassed.
            //
            // When CustomRootTrust is configured, the user has explicitly provided their own
            // trust anchors. The platform's rejection is expected (it doesn't know about custom
            // roots) and the managed chain builder's assessment should be authoritative.
            bool ignorePlatformTrustManager =
                _sslAuthenticationOptions.CertificateContext?.Trust is not null
                || _sslAuthenticationOptions.CertificateChainPolicy?.TrustMode == X509ChainTrustMode.CustomRootTrust;

            SslPolicyErrors sslPolicyErrors = chainTrustedByPlatform || ignorePlatformTrustManager
                ? SslPolicyErrors.None
                : SslPolicyErrors.RemoteCertificateChainErrors;

            bool platformTrustIsAuthoritative = chainTrustedByPlatform && !ignorePlatformTrustManager;

            ProtocolToken alertToken = default;

            var isValid = VerifyRemoteCertificate(
                _sslAuthenticationOptions.CertValidationDelegate,
                _sslAuthenticationOptions.CertificateContext?.Trust,
                ref alertToken,
                ref sslPolicyErrors,
                out X509ChainStatusFlags chainStatus);

            if (platformTrustIsAuthoritative)
            {
                // The platform's trust manager (which respects network-security-config.xml)
                // already validated the certificate chain. The managed X509 chain builder may
                // report RemoteCertificateChainErrors because the root CA trusted by the platform
                // is not in the managed certificate store (e.g. PartialChain or UntrustedRoot).
                // Strip chain errors — the platform's assessment is authoritative for chain trust.
                //
                // When CustomRootTrust is configured, the managed chain builder's assessment
                // is authoritative and its chain errors are real, not false positives.
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;

                if (!isValid && sslPolicyErrors == SslPolicyErrors.None)
                {
                    // The connection was rejected only because of chain errors that the platform
                    // already validated. Re-evaluate: accept if there's no user callback, or
                    // re-invoke the user callback with the corrected errors.
                    isValid = _sslAuthenticationOptions.CertValidationDelegate?.Invoke(
                        this, _remoteCertificate, null, sslPolicyErrors) ?? true;
                }
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
