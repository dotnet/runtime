// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private JavaProxy.RemoteCertificateValidationResult VerifyRemoteCertificate(IntPtr platformValidationError)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            if (ShouldRespectPlatformValidation() && platformValidationError != IntPtr.Zero)
            {
                sslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;

                // The Android trust manager only tells us whether the chain is trusted, not why it
                // was rejected, and that reason does not map cleanly onto SslPolicyErrors or the
                // X509Chain element statuses. Surface the platform's textual reason via NetEventSource
                // so it remains observable (e.g. through dotnet-trace) during development. Defer the
                // string marshalling until we know a listener is attached.
                if (NetEventSource.Log.IsEnabled())
                {
                    string? validationError = Interop.AndroidCrypto.GetPlatformValidationError(platformValidationError);
                    NetEventSource.Error(this, $"The Android platform trust manager rejected the remote certificate chain: {validationError}");
                }
            }

            ProtocolToken alertToken = default;

            var isValid = VerifyRemoteCertificate(
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

        private bool ShouldRespectPlatformValidation()
        {
            // Android platform trust is part of default/callback-only validation, but explicit
            // managed custom trust stays managed-authoritative and is not projected into Android.
            return _sslAuthenticationOptions.CertificateChainPolicy is not null
                ? _sslAuthenticationOptions.CertificateChainPolicy.TrustMode != X509ChainTrustMode.CustomRootTrust
                : _sslAuthenticationOptions.CertificateContext?.Trust is null;
        }

        private IntPtr SelectClientCertificateForHandshake(string[] acceptableIssuers)
        {
            X509Certificate2? selectedCertificate = SelectClientCertificate(acceptableIssuers);
            if (selectedCertificate is null)
            {
                return IntPtr.Zero;
            }

            if (_sslAuthenticationOptions.CertificateContext is null)
            {
                _sslAuthenticationOptions.SetCertificateContextFromCert(selectedCertificate);
            }

            return SafeDeleteSslContext.CreateKeyManagers(_sslAuthenticationOptions.CertificateContext!);
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
            private GCHandle<JavaProxy> _handle;

            public IntPtr Handle
                => _handle.IsAllocated
                    ? GCHandle<JavaProxy>.ToIntPtr(_handle)
                    : throw new ObjectDisposedException(nameof(JavaProxy));

            public Exception? ValidationException { get; private set; }
            public Exception? CertificateSelectionException { get; private set; }
            public RemoteCertificateValidationResult? ValidationResult { get; private set; }

            public JavaProxy(SslStream sslStream)
            {
                RegisterCallbacks();

                _sslStream = sslStream;
                _handle = new GCHandle<JavaProxy>(this);
            }

            public void Dispose()
            {
                _handle.Dispose();
            }

            private static unsafe void RegisterCallbacks()
            {
                if (!s_initialized)
                {
                    Interop.AndroidCrypto.RegisterSslStreamCallbacks(
                        &VerifyRemoteCertificate,
                        &SelectClientCertificate);
                    s_initialized = true;
                }
            }

            [UnmanagedCallersOnly]
            private static bool VerifyRemoteCertificate(IntPtr sslStreamProxyHandle, IntPtr platformValidationError)
            {
                JavaProxy? proxy = null;

                try
                {
                    proxy = GCHandle<JavaProxy>.FromIntPtr(sslStreamProxyHandle).Target;
                    Debug.Assert(proxy.ValidationResult is null);
                    proxy.ValidationResult = proxy._sslStream.VerifyRemoteCertificate(platformValidationError);
                    return proxy.ValidationResult.IsValid;
                }
                catch (Exception exception)
                {
                    proxy?.ValidationException = exception;
                    return false;
                }
            }

            [UnmanagedCallersOnly]
            private static unsafe IntPtr SelectClientCertificate(
                IntPtr sslStreamProxyHandle,
                int acceptableIssuerCount,
                IntPtr* acceptableIssuers)
            {
                JavaProxy? proxy = null;

                try
                {
                    proxy = GCHandle<JavaProxy>.FromIntPtr(sslStreamProxyHandle).Target;
                    Debug.Assert(proxy.CertificateSelectionException is null);
                    string[] issuers = new string[acceptableIssuerCount];
                    for (int i = 0; i < issuers.Length; i++)
                    {
                        issuers[i] = Marshal.PtrToStringUni(acceptableIssuers[i])!;
                    }

                    return proxy._sslStream.SelectClientCertificateForHandshake(issuers);
                }
                catch (Exception exception)
                {
                    proxy?.CertificateSelectionException = exception;
                    return IntPtr.Zero;
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
