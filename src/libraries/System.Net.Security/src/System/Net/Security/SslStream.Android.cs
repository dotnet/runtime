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
            // The platform's trust verdict is combined with managed validation to be MORE strict,
            // never less. If the platform rejects the chain, sslPolicyErrors is pre-seeded with
            // RemoteCertificateChainErrors and managed validation cannot clear it. If the platform
            // accepts the chain, managed validation (X509Chain.Build) can still independently
            // introduce RemoteCertificateChainErrors.
            //
            // The platform's verdict is ignored when the user provided intermediate certificates
            // via CertificateChainPolicy.ExtraStore. The platform does not have access to these
            // intermediates (Java's KeyStore.setCertificateEntry would elevate them to trust
            // anchors) and may produce false rejections for chains that require them. The managed
            // chain builder has full access to ExtraStore and is authoritative in this case.
            //
            // Note: ExtraStore is also populated later (in SslStream.Protocol.cs) with peer
            // certificates received during the TLS handshake. Those are the same certificates
            // the platform already has, so they don't affect this decision. At this point,
            // ExtraStore.Count reflects only user-provided certificates because
            // SslAuthenticationOptions.UpdateOptions clones the user's CertificateChainPolicy
            // for each handshake — peer certs from previous handshakes are never carried over.
            bool managedTrustOnly = _sslAuthenticationOptions.CertificateChainPolicy?.ExtraStore?.Count > 0;

            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            if (!managedTrustOnly && !chainTrustedByPlatform)
            {
                sslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }

            ProtocolToken alertToken = default;

            var isValid = VerifyRemoteCertificate(
                _sslAuthenticationOptions.CertValidationDelegate,
                _sslAuthenticationOptions.CertificateContext?.Trust,
                ref alertToken,
                ref sslPolicyErrors,
                out X509ChainStatusFlags chainStatus);

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
