// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal class SslAuthenticationOptions
    {
        internal SslAuthenticationOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertificateValidationCallback? remoteCallback, LocalCertSelectionCallback? localCallback)
        {
            Debug.Assert(sslClientAuthenticationOptions.TargetHost != null);

            // Common options.
            AllowRenegotiation = sslClientAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslClientAuthenticationOptions.ApplicationProtocols;
            CertValidationDelegate = remoteCallback;
            CheckCertName = true;
            EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslClientAuthenticationOptions.EnabledSslProtocols);
            EncryptionPolicy = sslClientAuthenticationOptions.EncryptionPolicy;
            IsServer = false;
            RemoteCertRequired = true;
            TargetHost = sslClientAuthenticationOptions.TargetHost!;

            // Client specific options.
            CertSelectionDelegate = localCallback;
            CertificateRevocationCheckMode = sslClientAuthenticationOptions.CertificateRevocationCheckMode;
            ClientCertificates = sslClientAuthenticationOptions.ClientCertificates;
            CipherSuitesPolicy = sslClientAuthenticationOptions.CipherSuitesPolicy;
        }

        internal SslAuthenticationOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            // Common options.
            AllowRenegotiation = sslServerAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols;
            CheckCertName = false;
            EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslServerAuthenticationOptions.EnabledSslProtocols);
            EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
            IsServer = true;
            RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"Server RemoteCertRequired: {RemoteCertRequired}.");
            }
            TargetHost = string.Empty;

            // Server specific options.
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
                    certificateWithKey = SecureChannel.FindCertificateWithPrivateKey(this, true, sslServerAuthenticationOptions.ServerCertificate);
                    if (certificateWithKey == null)
                    {
                        throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
                    }

                    CertificateContext = SslStreamCertificateContext.Create(certificateWithKey);
                }
            }

            if (sslServerAuthenticationOptions.RemoteCertificateValidationCallback != null)
            {
                CertValidationDelegate = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
            }
        }

        internal SslAuthenticationOptions(ServerOptionsSelectionCallback optionCallback, object? state, RemoteCertificateValidationCallback? remoteCallback)
        {
            CheckCertName = false;
            TargetHost = string.Empty;
            IsServer = true;
            UserState = state;
            ServerOptionDelegate = optionCallback;
            CertValidationDelegate = remoteCallback;
        }

        internal void UpdateOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
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
            else if (sslServerAuthenticationOptions.ServerCertificate is X509Certificate2 certificateWithKey &&
                    certificateWithKey.HasPrivateKey)
            {
                // given cert is X509Certificate2 with key. We can use it directly.
                CertificateContext = SslStreamCertificateContext.Create(certificateWithKey);
            }

            if (sslServerAuthenticationOptions.RemoteCertificateValidationCallback != null)
            {
                CertValidationDelegate = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
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
        internal SslStreamCertificateContext? CertificateContext { get; set; }
        internal SslProtocols EnabledSslProtocols { get; set; }
        internal X509RevocationMode CertificateRevocationCheckMode { get; set; }
        internal EncryptionPolicy EncryptionPolicy { get; set; }
        internal bool RemoteCertRequired { get; set; }
        internal bool CheckCertName { get; set; }
        internal RemoteCertificateValidationCallback? CertValidationDelegate { get; set; }
        internal LocalCertSelectionCallback? CertSelectionDelegate { get; set; }
        internal ServerCertSelectionCallback? ServerCertSelectionDelegate { get; set; }
        internal CipherSuitesPolicy? CipherSuitesPolicy { get; set; }
        internal object? UserState { get; }
        internal ServerOptionsSelectionCallback? ServerOptionDelegate { get; }
    }
}
