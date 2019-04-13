// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal class SslAuthenticationOptions
    {
        internal SslAuthenticationOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertValidationCallback remoteCallback, LocalCertSelectionCallback localCallback)
        {
            // Common options.
            AllowRenegotiation = sslClientAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslClientAuthenticationOptions.ApplicationProtocols;
            CertValidationDelegate = remoteCallback;
            CheckCertName = true;
            EnabledSslProtocols = sslClientAuthenticationOptions.EnabledSslProtocols;
            EncryptionPolicy = sslClientAuthenticationOptions.EncryptionPolicy;
            IsServer = false;
            RemoteCertRequired = true;
            TargetHost = sslClientAuthenticationOptions.TargetHost;

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
            EnabledSslProtocols = sslServerAuthenticationOptions.EnabledSslProtocols;
            EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
            IsServer = true;
            RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Info(this, $"Server RemoteCertRequired: {RemoteCertRequired}.");
            }
            TargetHost = string.Empty;

            // Server specific options.
            CertificateRevocationCheckMode = sslServerAuthenticationOptions.CertificateRevocationCheckMode;
            ServerCertificate = sslServerAuthenticationOptions.ServerCertificate;
            CipherSuitesPolicy = sslServerAuthenticationOptions.CipherSuitesPolicy;
        }

        internal bool AllowRenegotiation { get; set; }
        internal string TargetHost { get; set; }
        internal X509CertificateCollection ClientCertificates { get; set; }
        internal List<SslApplicationProtocol> ApplicationProtocols { get; }
        internal bool IsServer { get; set; }
        internal X509Certificate ServerCertificate { get; set; }
        internal SslProtocols EnabledSslProtocols { get; set; }
        internal X509RevocationMode CertificateRevocationCheckMode { get; set; }
        internal EncryptionPolicy EncryptionPolicy { get; set; }
        internal bool RemoteCertRequired { get; set; }
        internal bool CheckCertName { get; set; }
        internal RemoteCertValidationCallback CertValidationDelegate { get; set; }
        internal LocalCertSelectionCallback CertSelectionDelegate { get; set; }
        internal ServerCertSelectionCallback ServerCertSelectionDelegate { get; set; }
        internal CipherSuitesPolicy CipherSuitesPolicy { get; set; }
    }
}

