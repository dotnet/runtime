// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

public partial class QuicConnection
{
    private readonly struct SslConnectionOptions
    {
        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", null);
        private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", null);

        /// <summary>
        /// Determines if the connection is outbound/client or inbound/server.
        /// </summary>
        private readonly bool _isClient;
        /// <summary>
        /// Host name send in SNI, set only for outbound/client connections. Configured via <see cref="SslClientAuthenticationOptions.TargetHost"/>.
        /// </summary>
        private readonly string? _targetHost;
        /// <summary>
        /// Always <c>true</c> for outbound/client connections. Configured for inbound/server ones via <see cref="SslServerAuthenticationOptions.ClientCertificateRequired"/>.
        /// </summary>
        private readonly bool _certificateRequired;
        /// <summary>
        /// Configured via <see cref="SslServerAuthenticationOptions.CertificateRevocationCheckMode"/> or <see cref="SslClientAuthenticationOptions.CertificateRevocationCheckMode"/>.
        /// </summary>
        private readonly X509RevocationMode _revocationMode;
        /// <summary>
        /// Configured via <see cref="SslServerAuthenticationOptions.RemoteCertificateValidationCallback"/> or <see cref="SslClientAuthenticationOptions.RemoteCertificateValidationCallback"/>.
        /// </summary>
        private readonly RemoteCertificateValidationCallback? _validationCallback;

        public SslConnectionOptions(bool isClient, string? targetHost, bool certificateRequired, X509RevocationMode revocationMode, RemoteCertificateValidationCallback? validationCallback)
        {
            _isClient = isClient;
            _targetHost = targetHost;
            _certificateRequired = certificateRequired;
            _revocationMode = revocationMode;
            _validationCallback = validationCallback;
        }

        public unsafe int ValidateCertificate(QUIC_BUFFER* certificatePtr, QUIC_BUFFER* chainPtr, out X509Certificate2? certificate)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            X509Chain? chain = null;
            IntPtr certificateBuffer = default;
            int certificateLength = default;

            certificate = null;

            if (certificatePtr is not null)
            {
                chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = _revocationMode;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.ApplicationPolicy.Add(_isClient ? s_serverAuthOid : s_clientAuthOid);

                if (OperatingSystem.IsWindows())
                {
                    certificate = new X509Certificate2((IntPtr)certificatePtr);
                }
                else
                {
                    if (certificatePtr->Length > 0)
                    {
                        certificateBuffer = (IntPtr)certificatePtr->Buffer;
                        certificateLength = (int)certificatePtr->Length;
                        certificate = new X509Certificate2(certificatePtr->Span);
                    }
                    if (chainPtr->Length > 0)
                    {
                        X509Certificate2Collection additionalCertificates = new X509Certificate2Collection();
                        additionalCertificates.Import(chainPtr->Span);
                        chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
                    }
                }
            }

            if (certificate is not null)
            {
                sslPolicyErrors |= CertificateValidation.BuildChainAndVerifyProperties(chain!, certificate, true, !_isClient, _targetHost, certificateBuffer, certificateLength);
            }

            if (certificate is null && _certificateRequired)
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
            }

            if (_validationCallback is not null)
            {
                if (!_validationCallback(this, certificate, chain, sslPolicyErrors))
                {
                    if (_isClient)
                    {
                        throw new AuthenticationException(SR.net_quic_cert_custom_validation);
                    }
                    return QUIC_STATUS_USER_CANCELED;
                }
                return QUIC_STATUS_SUCCESS;
            }

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                if (_isClient)
                {
                    throw new AuthenticationException(SR.Format(SR.net_quic_cert_chain_validation, sslPolicyErrors));
                }
                return QUIC_STATUS_HANDSHAKE_FAILURE;
            }

            return QUIC_STATUS_SUCCESS;
        }
    }
}
