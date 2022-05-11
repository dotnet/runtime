// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal sealed class SslAuthenticationOptions
    {
        internal SslAuthenticationOptions()
        {
            TargetHost = string.Empty;
        }

        internal void UpdateOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            if (CertValidationDelegate == null)
            {
                CertValidationDelegate = sslClientAuthenticationOptions.RemoteCertificateValidationCallback;
            }
            else if (sslClientAuthenticationOptions.RemoteCertificateValidationCallback != null &&
                     CertValidationDelegate != sslClientAuthenticationOptions.RemoteCertificateValidationCallback)
            {
                // Callback was set in constructor to different value.
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(RemoteCertificateValidationCallback)));
            }

            if (CertSelectionDelegate == null)
            {
                CertSelectionDelegate = sslClientAuthenticationOptions.LocalCertificateSelectionCallback;
            }
            else if (sslClientAuthenticationOptions.LocalCertificateSelectionCallback != null &&
                     CertSelectionDelegate != sslClientAuthenticationOptions.LocalCertificateSelectionCallback)
            {
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(LocalCertificateSelectionCallback)));
            }

            // Common options.
            AllowRenegotiation = sslClientAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslClientAuthenticationOptions.ApplicationProtocols;
            CheckCertName = true;
            EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslClientAuthenticationOptions.EnabledSslProtocols);
            EncryptionPolicy = sslClientAuthenticationOptions.EncryptionPolicy;
            IsServer = false;
            RemoteCertRequired = true;
            // RFC 6066 section 3 says to exclude trailing dot from fully qualified DNS hostname
            if (sslClientAuthenticationOptions.TargetHost != null)
            {
                TargetHost = sslClientAuthenticationOptions.TargetHost.TrimEnd('.');
            }

            // Client specific options.
            CertificateRevocationCheckMode = sslClientAuthenticationOptions.CertificateRevocationCheckMode;
            ClientCertificates = sslClientAuthenticationOptions.ClientCertificates;
            CipherSuitesPolicy = sslClientAuthenticationOptions.CipherSuitesPolicy;
        }

        internal void UpdateOptions(ServerOptionsSelectionCallback optionCallback, object? state)
        {
            CheckCertName = false;
            TargetHost = string.Empty;
            IsServer = true;
            UserState = state;
            ServerOptionDelegate = optionCallback;
        }

        internal void UpdateOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            if (sslServerAuthenticationOptions.ServerCertificate == null &&
                sslServerAuthenticationOptions.ServerCertificateContext == null &&
                sslServerAuthenticationOptions.ServerCertificateSelectionCallback == null &&
                CertSelectionDelegate == null)
            {
                throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
            }

            if ((sslServerAuthenticationOptions.ServerCertificate != null ||
                 sslServerAuthenticationOptions.ServerCertificateContext != null ||
                 CertSelectionDelegate != null) &&
                sslServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
            {
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(ServerCertificateSelectionCallback)));
            }

            if (CertValidationDelegate == null)
            {
                CertValidationDelegate = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
            }
            else if (sslServerAuthenticationOptions.RemoteCertificateValidationCallback != null &&
                     CertValidationDelegate != sslServerAuthenticationOptions.RemoteCertificateValidationCallback)
            {
                // Callback was set in constructor to differet value.
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(RemoteCertificateValidationCallback)));
            }

            IsServer = true;
            AllowRenegotiation = sslServerAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols;
            EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslServerAuthenticationOptions.EnabledSslProtocols);
            EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
            RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
            CipherSuitesPolicy = sslServerAuthenticationOptions.CipherSuitesPolicy;
            CertificateRevocationCheckMode = sslServerAuthenticationOptions.CertificateRevocationCheckMode;
            if (sslServerAuthenticationOptions.ServerCertificateContext != null)
            {
                CertificateContext = sslServerAuthenticationOptions.ServerCertificateContext;
            }
            else if (sslServerAuthenticationOptions.ServerCertificate != null)
            {
                X509Certificate2? certificateWithKey = sslServerAuthenticationOptions.ServerCertificate as X509Certificate2;

                if (certificateWithKey != null && certificateWithKey.HasPrivateKey)
                {
                    // given cert is X509Certificate2 with key. We can use it directly.
                    CertificateContext = SslStreamCertificateContext.Create(certificateWithKey, null);
                }
                else
                {
                    // This is legacy fix-up. If the Certificate did not have key, we will search stores and we
                    // will try to find one with matching hash.
                    certificateWithKey = SslStream.FindCertificateWithPrivateKey(this, true, sslServerAuthenticationOptions.ServerCertificate);
                    if (certificateWithKey == null)
                    {
                        throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
                    }

                    CertificateContext = SslStreamCertificateContext.Create(certificateWithKey);
                }
            }

            if (sslServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
            {
                ServerCertSelectionDelegate = sslServerAuthenticationOptions.ServerCertificateSelectionCallback;
            }
        }

        private static SslProtocols FilterOutIncompatibleSslProtocols(SslProtocols protocols)
        {
            if (protocols.HasFlag(SslProtocols.Tls12) || protocols.HasFlag(SslProtocols.Tls13))
            {
#pragma warning disable 0618
                // SSL2 is mutually exclusive with >= TLS1.2
                // On Windows10 SSL2 flag has no effect but on earlier versions of the OS
                // opting into both SSL2 and >= TLS1.2 causes negotiation to always fail.
                protocols &= ~SslProtocols.Ssl2;
#pragma warning restore 0618
            }

            return protocols;
        }

        internal bool AllowRenegotiation { get; set; }
        internal string TargetHost { get; set; }
        internal X509CertificateCollection? ClientCertificates { get; set; }
        internal List<SslApplicationProtocol>? ApplicationProtocols { get; set; }
        internal bool IsServer { get; set; }
        internal bool IsClient => !IsServer;
        internal SslStreamCertificateContext? CertificateContext { get; set; }
        internal SslProtocols EnabledSslProtocols { get; set; }
        internal X509RevocationMode CertificateRevocationCheckMode { get; set; }
        internal EncryptionPolicy EncryptionPolicy { get; set; }
        internal bool RemoteCertRequired { get; set; }
        internal bool CheckCertName { get; set; }
        internal RemoteCertificateValidationCallback? CertValidationDelegate { get; set; }
        internal LocalCertificateSelectionCallback? CertSelectionDelegate { get; set; }
        internal ServerCertificateSelectionCallback? ServerCertSelectionDelegate { get; set; }
        internal CipherSuitesPolicy? CipherSuitesPolicy { get; set; }
        internal object? UserState { get; set; }
        internal ServerOptionsSelectionCallback? ServerOptionDelegate { get; set; }
    }
}
