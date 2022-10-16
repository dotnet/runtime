// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal sealed class RemoteCertificateVerification
    {
        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");
        private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");

        private readonly SslStream _sslStream;
        private readonly SslAuthenticationOptions _sslAuthenticationOptions;
        private readonly SafeDeleteSslContext _securityContext;

        public RemoteCertificateVerification(
            SslStream sslStream,
            SslAuthenticationOptions sslAuthenticationOptions,
            SafeDeleteSslContext securityContext)
        {
            _sslStream = sslStream;
            _sslAuthenticationOptions = sslAuthenticationOptions;
            _securityContext = securityContext;
        }

        internal bool VerifyRemoteCertificate(
            X509Certificate2? remoteCertificate,
            SslCertificateTrust? trust,
            X509Chain? chain,
            out SslPolicyErrors sslPolicyErrors,
            out X509ChainStatus[] chainStatus)
        {
            bool success = false;
            sslPolicyErrors = SslPolicyErrors.None;
            chainStatus = Array.Empty<X509ChainStatus>();

            try
            {
                if (remoteCertificate == null)
                {
                    if (NetEventSource.Log.IsEnabled() && _sslAuthenticationOptions.RemoteCertRequired)
                        NetEventSource.Error(_sslStream, $"Remote certificate required, but no remote certificate received");

                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                }
                else
                {
                    chain ??= new X509Chain();

                    if (_sslAuthenticationOptions.CertificateChainPolicy != null)
                    {
                        chain.ChainPolicy = _sslAuthenticationOptions.CertificateChainPolicy;
                    }
                    else
                    {
                        chain.ChainPolicy.RevocationMode = _sslAuthenticationOptions.CertificateRevocationCheckMode;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                        if (trust != null)
                        {
                            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                            if (trust._store != null)
                            {
                                chain.ChainPolicy.CustomTrustStore.AddRange(trust._store.Certificates);
                            }
                            if (trust._trustList != null)
                            {
                                chain.ChainPolicy.CustomTrustStore.AddRange(trust._trustList);
                            }
                        }
                    }

                    // set ApplicationPolicy unless already provided.
                    if (chain.ChainPolicy.ApplicationPolicy.Count == 0)
                    {
                        // Authenticate the remote party: (e.g. when operating in server mode, authenticate the client).
                        chain.ChainPolicy.ApplicationPolicy.Add(_sslAuthenticationOptions.IsServer ? s_clientAuthOid : s_serverAuthOid);
                    }

                    sslPolicyErrors |= CertificateValidationPal.VerifyCertificateProperties(
                        _securityContext,
                        chain,
                        remoteCertificate,
                        _sslAuthenticationOptions.CheckCertName,
                        _sslAuthenticationOptions.IsServer,
                        _sslAuthenticationOptions.TargetHost);
                }

                var remoteCertValidationCallback = _sslAuthenticationOptions.CertValidationDelegate;
                if (remoteCertValidationCallback != null)
                {
                    // the validation callback has already been called by the trust manager
                    success = remoteCertValidationCallback(_sslStream, remoteCertificate, chain, sslPolicyErrors);
                }
                else
                {
                    if (!_sslAuthenticationOptions.RemoteCertRequired)
                    {
                        sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
                    }

                    success = (sslPolicyErrors == SslPolicyErrors.None);
                }

                LogCertificateValidationResult(remoteCertificate, chain, success, sslPolicyErrors, remoteCertValidationCallback);

                if (!success && chain != null)
                {
                    chainStatus = chain.ChainStatus;
                }
            }
            finally
            {
                // At least on Win2k server the chain is found to have dependencies on the original cert context.
                // So it should be closed first.

                if (chain != null)
                {
                    int elementsCount = chain.ChainElements.Count;
                    for (int i = 0; i < elementsCount; i++)
                    {
                        chain.ChainElements[i].Certificate.Dispose();
                    }

                    chain.Dispose();
                }
            }

            return success;
        }

        private void LogCertificateValidationResult(
            X509Certificate2? remoteCertificate,
            X509Chain? chain,
            bool success,
            SslPolicyErrors sslPolicyErrors,
            RemoteCertificateValidationCallback? remoteCertValidationCallback)
        {
            if (!NetEventSource.Log.IsEnabled())
                return;

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                NetEventSource.Log.RemoteCertificateError(_sslStream, SR.net_log_remote_cert_has_errors);
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                {
                    NetEventSource.Log.RemoteCertificateError(_sslStream, SR.net_log_remote_cert_not_available);
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    NetEventSource.Log.RemoteCertificateError(_sslStream, SR.net_log_remote_cert_name_mismatch);
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && chain is not null)
                {
                    string chainStatusString = "ChainStatus: ";
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        chainStatusString += "\t" + chainStatus.StatusInformation;
                    }
                    NetEventSource.Log.RemoteCertificateError(_sslStream, chainStatusString);
                }
            }

            if (success)
            {
                if (remoteCertValidationCallback != null)
                {
                    NetEventSource.Log.RemoteCertDeclaredValid(_sslStream);
                }
                else
                {
                    NetEventSource.Log.RemoteCertHasNoErrors(_sslStream);
                }
            }
            else
            {
                if (remoteCertValidationCallback != null)
                {
                    NetEventSource.Log.RemoteCertUserDeclaredInvalid(_sslStream);
                }
            }

            NetEventSource.Info(_sslStream, $"Cert validation, remote cert = {remoteCertificate}");
        }
    }
}
