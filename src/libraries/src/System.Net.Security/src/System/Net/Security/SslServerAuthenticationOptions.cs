// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public class SslServerAuthenticationOptions
    {
        private X509RevocationMode _checkCertificateRevocation = X509RevocationMode.NoCheck;
        private SslProtocols _enabledSslProtocols = SecurityProtocol.SystemDefaultSecurityProtocols;
        private EncryptionPolicy _encryptionPolicy = EncryptionPolicy.RequireEncryption;
        private bool _allowRenegotiation = true;

        public bool AllowRenegotiation
        {
            get => _allowRenegotiation;
            set => _allowRenegotiation = value;
        }

        public bool ClientCertificateRequired { get; set; }

        public List<SslApplicationProtocol> ApplicationProtocols { get; set; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public ServerCertificateSelectionCallback ServerCertificateSelectionCallback { get; set; }

        public X509Certificate ServerCertificate { get; set; }

        public SslProtocols EnabledSslProtocols
        {
            get => _enabledSslProtocols;
            set => _enabledSslProtocols = value;
        }

        public X509RevocationMode CertificateRevocationCheckMode
        {
            get => _checkCertificateRevocation;
            set
            {
                if (value != X509RevocationMode.NoCheck && value != X509RevocationMode.Offline && value != X509RevocationMode.Online)
                {
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, nameof(X509RevocationMode)), nameof(value));
                }

                _checkCertificateRevocation = value;
            }
        }

        public EncryptionPolicy EncryptionPolicy
        {
            get => _encryptionPolicy;
            set
            {
                if (value != EncryptionPolicy.RequireEncryption && value != EncryptionPolicy.AllowNoEncryption && value != EncryptionPolicy.NoEncryption)
                {
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, nameof(EncryptionPolicy)), nameof(value));
                }

                _encryptionPolicy = value;
            }
        }

        /// <summary>
        /// Specifies cipher suites allowed to be used for TLS.
        /// When set to null operating system default will be used.
        /// Use extreme caution when changing this setting.
        /// </summary>
        public CipherSuitesPolicy CipherSuitesPolicy { get; set; }
    }
}

