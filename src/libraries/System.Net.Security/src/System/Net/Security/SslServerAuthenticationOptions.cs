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
        private X509Certificate _serverCertificate;
        private X509RevocationMode _checkCertificateRevocation = X509RevocationMode.NoCheck;
        private SslProtocols _enabledSslProtocols = SecurityProtocol.SystemDefaultSecurityProtocols;
        private EncryptionPolicy _encryptionPolicy = EncryptionPolicy.RequireEncryption;

        internal RemoteCertValidationCallback _certValidationDelegate;
        internal LocalCertSelectionCallback _certSelectionDelegate;

        public bool AllowRenegotiation { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public IList<SslApplicationProtocol> ApplicationProtocols { get; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get;set; }

        public X509Certificate ServerCertificate
        {
            get => _serverCertificate;
            set => _serverCertificate = value ?? throw new ArgumentNullException(nameof(value));
        }

        public SslProtocols EnabledSslProtocols
        {
            get => _enabledSslProtocols;
            set => _enabledSslProtocols = value;
        }

        public X509RevocationMode CheckCertificateRevocation
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
    }
}

