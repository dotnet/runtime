// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
        /// The connection to which these options belong.
        /// </summary>
        private readonly QuicConnection _connection;
        /// <summary>
        /// Determines if the connection is outbound/client or inbound/server.
        /// </summary>
        private readonly bool _isClient;
        /// <summary>
        /// Host name send in SNI, set only for outbound/client connections. Configured via <see cref="SslClientAuthenticationOptions.TargetHost"/>.
        /// </summary>
        private readonly string _targetHost;
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

        /// <summary>
        /// Configured via <see cref="SslServerAuthenticationOptions.CertificateChainPolicy"/> or <see cref="SslClientAuthenticationOptions.CertificateChainPolicy"/>.
        /// </summary>
        private readonly X509ChainPolicy? _certificateChainPolicy;

        internal string TargetHost => _targetHost;

        public SslConnectionOptions(QuicConnection connection, bool isClient,
            string targetHost, bool certificateRequired, X509RevocationMode
            revocationMode, RemoteCertificateValidationCallback? validationCallback,
            X509ChainPolicy? certificateChainPolicy)
        {
            _connection = connection;
            _isClient = isClient;
            _targetHost = targetHost;
            _certificateRequired = certificateRequired;
            _revocationMode = revocationMode;
            _validationCallback = validationCallback;
            _certificateChainPolicy = certificateChainPolicy;
        }

        internal async Task<bool> StartAsyncCertificateValidation(IntPtr certificatePtr, IntPtr chainPtr)
        {
            //
            // The provided data pointers are valid only while still inside this function, so they need to be
            // copied to separate buffers which are then handed off to threadpool.
            //

            X509Certificate2? certificate = null;

            byte[]? certDataRented = null;
            Memory<byte> certData = default;
            byte[]? chainDataRented = null;
            Memory<byte> chainData = default;

            if (certificatePtr != IntPtr.Zero)
            {
                if (MsQuicApi.UsesSChannelBackend)
                {
                    // provided data is a pointer to a CERT_CONTEXT
                    certificate = new X509Certificate2(certificatePtr);
                    // TODO: what about chainPtr?
                }
                else
                {
                    unsafe
                    {
                        // On non-SChannel backends we specify USE_PORTABLE_CERTIFICATES and the contents are buffers
                        // with DER encoded cert and chain.
                        QUIC_BUFFER* certificateBuffer = (QUIC_BUFFER*)certificatePtr;
                        QUIC_BUFFER* chainBuffer = (QUIC_BUFFER*)chainPtr;

                        if (certificateBuffer->Length > 0)
                        {
                            certDataRented = ArrayPool<byte>.Shared.Rent((int)certificateBuffer->Length);
                            certData = certDataRented.AsMemory(0, (int)certificateBuffer->Length);
                            certificateBuffer->Span.CopyTo(certData.Span);
                        }

                        if (chainBuffer->Length > 0)
                        {
                            chainDataRented = ArrayPool<byte>.Shared.Rent((int)chainBuffer->Length);
                            chainData = chainDataRented.AsMemory(0, (int)chainBuffer->Length);
                            chainBuffer->Span.CopyTo(chainData.Span);
                        }
                    }
                }
            }

            // We wan't to do the certificate validation asynchronously, but due to a bug in MsQuic, we need to call the callback synchronously on some versions
            if (MsQuicApi.SupportsAsyncCertValidation)
            {
                // force yield to the thread pool to free up MsQuic worker thread.
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }

            // certificatePtr and chainPtr are invalid beyond this point

            QUIC_TLS_ALERT_CODES result;
            try
            {
                if (certData.Length > 0)
                {
                    Debug.Assert(certificate == null);
                    certificate = new X509Certificate2(certData.Span);
                }

                result = _connection._sslConnectionOptions.ValidateCertificate(certificate, certData.Span, chainData.Span);
                _connection._remoteCertificate = certificate;
            }
            catch (Exception ex)
            {
                certificate?.Dispose();
                _connection._connectedTcs.TrySetException(ex);
                result = QUIC_TLS_ALERT_CODES.USER_CANCELED;
            }
            finally
            {
                if (certDataRented != null)
                {
                    ArrayPool<byte>.Shared.Return(certDataRented);
                }

                if (chainDataRented != null)
                {
                    ArrayPool<byte>.Shared.Return(chainDataRented);
                }
            }

            if (MsQuicApi.SupportsAsyncCertValidation)
            {
                int status = MsQuicApi.Api.ConnectionCertificateValidationComplete(
                    _connection._handle,
                    result == QUIC_TLS_ALERT_CODES.SUCCESS ? (byte)1 : (byte)0,
                    result);

                if (MsQuic.StatusFailed(status))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Error(_connection, $"{_connection} ConnectionCertificateValidationComplete failed with {ThrowHelper.GetErrorMessageForStatus(status)}");
                    }
                }
            }

            return result == QUIC_TLS_ALERT_CODES.SUCCESS;
        }

        private QUIC_TLS_ALERT_CODES ValidateCertificate(X509Certificate2? certificate, Span<byte> certData, Span<byte> chainData)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            bool wrapException = false;

            X509Chain? chain = null;
            try
            {
                if (certificate is not null)
                {
                    chain = new X509Chain();
                    if (_certificateChainPolicy != null)
                    {
                        chain.ChainPolicy = _certificateChainPolicy;
                    }
                    else
                    {
                        chain.ChainPolicy.RevocationMode = _revocationMode;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                        // TODO: configure chain.ChainPolicy.CustomTrustStore to mirror behavior of SslStream.VerifyRemoteCertificate (https://github.com/dotnet/runtime/issues/73053)
                    }

                    // set ApplicationPolicy unless already provided.
                    if (chain.ChainPolicy.ApplicationPolicy.Count == 0)
                    {
                        // Authenticate the remote party: (e.g. when operating in server mode, authenticate the client).
                        chain.ChainPolicy.ApplicationPolicy.Add(_isClient ? s_serverAuthOid : s_clientAuthOid);
                    }

                    if (chainData.Length > 0)
                    {
                        X509Certificate2Collection additionalCertificates = new X509Certificate2Collection();
                        additionalCertificates.Import(chainData);
                        chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
                    }

                    bool checkCertName = !chain!.ChainPolicy!.VerificationFlags.HasFlag(X509VerificationFlags.IgnoreInvalidName);
                    sslPolicyErrors |= CertificateValidation.BuildChainAndVerifyProperties(chain!, certificate, checkCertName, !_isClient, TargetHostNameHelper.NormalizeHostName(_targetHost), certData);
                }
                else if (_certificateRequired)
                {
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                }

                QUIC_TLS_ALERT_CODES result = QUIC_TLS_ALERT_CODES.SUCCESS;
                if (_validationCallback is not null)
                {
                    wrapException = true;
                    if (!_validationCallback(_connection, certificate, chain, sslPolicyErrors))
                    {
                        wrapException = false;
                        if (_isClient)
                        {
                            throw new AuthenticationException(SR.net_quic_cert_custom_validation);
                        }

                        result = QUIC_TLS_ALERT_CODES.BAD_CERTIFICATE;
                    }
                }
                else if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    if (_isClient)
                    {
                        throw new AuthenticationException(SR.Format(SR.net_quic_cert_chain_validation, sslPolicyErrors));
                    }

                    result = QUIC_TLS_ALERT_CODES.BAD_CERTIFICATE;
                }

                return result;
            }
            catch (Exception ex)
            {
                if (wrapException)
                {
                    throw new QuicException(QuicError.CallbackError, null, SR.net_quic_callback_error, ex);
                }

                throw;
            }
            finally
            {
                if (chain is not null)
                {
                    X509ChainElementCollection elements = chain.ChainElements;
                    for (int i = 0; i < elements.Count; i++)
                    {
                        elements[i].Certificate.Dispose();
                    }

                    chain.Dispose();
                }
            }
        }
    }
}
