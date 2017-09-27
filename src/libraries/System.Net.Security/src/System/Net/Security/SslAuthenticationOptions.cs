// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal class SslAuthenticationOptions
    {
        internal SslAuthenticationOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            AllowRenegotiation = sslClientAuthenticationOptions.AllowRenegotiation;
            ApplicationProtocols = sslClientAuthenticationOptions.ApplicationProtocols;
            CheckCertificateRevocation = sslClientAuthenticationOptions.CheckCertificateRevocation;
            ClientCertificates = sslClientAuthenticationOptions.ClientCertificates;
            EnabledSslProtocols = sslClientAuthenticationOptions.EnabledSslProtocols;
            EncryptionPolicy = sslClientAuthenticationOptions.EncryptionPolicy;
            LocalCertificateSelectionCallback = sslClientAuthenticationOptions.LocalCertificateSelectionCallback;
            RemoteCertificateValidationCallback = sslClientAuthenticationOptions.RemoteCertificateValidationCallback;
            CertValidationDelegate = sslClientAuthenticationOptions._certValidationDelegate;
            CertSelectionDelegate = sslClientAuthenticationOptions._certSelectionDelegate;
            TargetHost = sslClientAuthenticationOptions.TargetHost;
            IsServer = false;
            CheckCertName = true;
            RemoteCertRequired = true;
        }

        internal SslAuthenticationOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            TargetHost = string.Empty;
            AllowRenegotiation = sslServerAuthenticationOptions.AllowRenegotiation;
            RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
            ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols;
            RemoteCertificateValidationCallback = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
            CertValidationDelegate = sslServerAuthenticationOptions._certValidationDelegate;
            ServerCertificate = sslServerAuthenticationOptions.ServerCertificate;
            EnabledSslProtocols = sslServerAuthenticationOptions.EnabledSslProtocols;
            CheckCertificateRevocation = sslServerAuthenticationOptions.CheckCertificateRevocation;
            EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
            CheckCertName = false;
            IsServer = true;
        }

        internal bool AllowRenegotiation { get; set; }
        internal string TargetHost { get; set; }
        internal X509CertificateCollection ClientCertificates { get; set; }
        internal IList<SslApplicationProtocol> ApplicationProtocols { get; }
        internal bool IsServer { get; set; }
        internal RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
        internal LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }
        internal X509Certificate ServerCertificate { get; set; }
        internal SslProtocols EnabledSslProtocols { get; set; }
        internal X509RevocationMode CheckCertificateRevocation { get; set; }
        internal EncryptionPolicy EncryptionPolicy { get; set; }
        internal bool RemoteCertRequired { get; set; }
        internal bool CheckCertName { get; set; }
        internal RemoteCertValidationCallback CertValidationDelegate { get; set; }
        internal LocalCertSelectionCallback CertSelectionDelegate { get; set; }
        internal GCHandle AlpnProtocolsHandle { get; set; }

        internal static byte[] ConvertAlpnProtocolListToByteArray(IList<SslApplicationProtocol> protocols)
        {
            int protocolSize = 0;
            foreach (SslApplicationProtocol protocol in protocols)
            {
                if (protocol.Protocol.Length <= 0 || protocol.Protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(protocols));
                }

                protocolSize += protocol.Protocol.Length + 1;
            }

            byte[] buffer = new byte[protocolSize];
            var offset = 0;
            foreach (SslApplicationProtocol protocol in protocols)
            {
                buffer[offset++] = (byte)(protocol.Protocol.Length);
                Array.Copy(protocol.Protocol.ToArray(), 0, buffer, offset, protocol.Protocol.Length);
                offset += protocol.Protocol.Length;
            }

            return buffer;
        }
    }
}

