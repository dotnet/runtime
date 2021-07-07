// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    // SecureChannel - a wrapper on SSPI based functionality.
    // Provides an additional abstraction layer over SSPI for SslStream.
    internal sealed class SecureChannel
    {
        // When reading a frame from the wire first read this many bytes for the header.
        internal const int ReadHeaderSize = 5;

        private SafeFreeCredentials? _credentialsHandle;
        private SafeDeleteSslContext? _securityContext;

        private SslConnectionInfo? _connectionInfo;
        private X509Certificate? _selectedClientCertificate;
        private X509Certificate2? _remoteCertificate;
        private bool _remoteCertificateExposed;

        // These are the MAX encrypt buffer output sizes, not the actual sizes.
        private int _headerSize = 5; //ATTN must be set to at least 5 by default
        private int _trailerSize = 16;
        private int _maxDataSize = 16354;

        private bool _refreshCredentialNeeded;

        private readonly SslAuthenticationOptions _sslAuthenticationOptions;
        private SslApplicationProtocol _negotiatedApplicationProtocol;

        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");
        private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");
        private SslStream? _ssl;

        internal SecureChannel(SslAuthenticationOptions sslAuthenticationOptions, SslStream sslStream)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.SecureChannelCtor(this, sslStream, sslAuthenticationOptions.TargetHost!, sslAuthenticationOptions.ClientCertificates, sslAuthenticationOptions.EncryptionPolicy);

            SslStreamPal.VerifyPackageInfo();
            Debug.Assert(sslAuthenticationOptions.TargetHost != null, "sslAuthenticationOptions.TargetHost == null");

            _securityContext = null;
            _refreshCredentialNeeded = true;
            _sslAuthenticationOptions = sslAuthenticationOptions;
            _ssl = sslStream;
        }

        //
        // SecureChannel properties
        //
        //   LocalServerCertificate - local certificate for server mode channel
        //   LocalClientCertificate - selected certificated used in the client channel mode otherwise null
        //   IsRemoteCertificateAvailable - true if the remote side has provided a certificate
        //   HeaderSize             - Header & trailer sizes used in the TLS stream
        //   TrailerSize -
        //
        internal X509Certificate? LocalServerCertificate
        {
            get
            {
                return _sslAuthenticationOptions.CertificateContext?.Certificate;
            }
        }

        internal X509Certificate? LocalClientCertificate
        {
            get
            {
                return _selectedClientCertificate;
            }
        }

        internal bool IsRemoteCertificateAvailable
        {
            get
            {
                return _remoteCertificate != null;
            }
        }

        internal X509Certificate? RemoteCertificate
        {
            get
            {
                _remoteCertificateExposed = true;
                return _remoteCertificate;
            }
        }

        internal ChannelBinding? GetChannelBinding(ChannelBindingKind kind)
        {
            ChannelBinding? result = null;
            if (_securityContext != null)
            {
                result = SslStreamPal.QueryContextChannelBinding(_securityContext, kind);
            }

            return result;
        }

        internal X509RevocationMode CheckCertRevocationStatus
        {
            get
            {
                return _sslAuthenticationOptions.CertificateRevocationCheckMode;
            }
        }

        internal int MaxDataSize
        {
            get
            {
                return _maxDataSize;
            }
        }

        internal SslConnectionInfo? ConnectionInfo
        {
            get
            {
                return _connectionInfo;
            }
        }

        internal bool IsValidContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return !(_securityContext == null || _securityContext.IsInvalid);
            }
        }

        internal bool IsServer
        {
            get
            {
                return _sslAuthenticationOptions.IsServer;
            }
        }

        internal bool RemoteCertRequired
        {
            get
            {
                return _sslAuthenticationOptions.RemoteCertRequired;
            }
        }

        internal SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                return _negotiatedApplicationProtocol;
            }
        }

        internal void SetRefreshCredentialNeeded()
        {
            _refreshCredentialNeeded = true;
        }

        internal void Close()
        {
            if (!_remoteCertificateExposed)
            {
                  _remoteCertificate?.Dispose();
                  _remoteCertificate = null;
            }

            _securityContext?.Dispose();
            _credentialsHandle?.Dispose();
            _ssl = null;
            GC.SuppressFinalize(this);
        }

        //
        // SECURITY: we open a private key container on behalf of the caller
        // and we require the caller to have permission associated with that operation.
        //
        internal static X509Certificate2? FindCertificateWithPrivateKey(object instance, bool isServer, X509Certificate certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Log.LocatingPrivateKey(certificate, instance);

            try
            {
                // Protecting from X509Certificate2 derived classes.
                X509Certificate2? certEx = MakeEx(certificate);

                if (certEx != null)
                {
                    if (certEx.HasPrivateKey)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.CertIsType2(instance);

                        return certEx;
                    }

                    if (!object.ReferenceEquals(certificate, certEx))
                    {
                        certEx.Dispose();
                    }
                }

                X509Certificate2Collection collectionEx;
                string certHash = certEx!.Thumbprint;

                // ELSE Try the MY user and machine stores for private key check.
                // For server side mode MY machine store takes priority.
                X509Store? store = CertificateValidationPal.EnsureStoreOpened(isServer);
                if (store != null)
                {
                    collectionEx = store.Certificates.Find(X509FindType.FindByThumbprint, certHash, false);
                    if (collectionEx.Count > 0 && collectionEx[0].HasPrivateKey)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.FoundCertInStore(isServer, instance);
                        return collectionEx[0];
                    }
                }

                store = CertificateValidationPal.EnsureStoreOpened(!isServer);
                if (store != null)
                {
                    collectionEx = store.Certificates.Find(X509FindType.FindByThumbprint, certHash, false);
                    if (collectionEx.Count > 0 && collectionEx[0].HasPrivateKey)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.FoundCertInStore(!isServer, instance);
                        return collectionEx[0];
                    }
                }
            }
            catch (CryptographicException)
            {
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Log.NotFoundCertInStore(instance);
            return null;
        }

        private static X509Certificate2? MakeEx(X509Certificate certificate)
        {
            Debug.Assert(certificate != null, "certificate != null");

            if (certificate.GetType() == typeof(X509Certificate2))
            {
                return (X509Certificate2)certificate;
            }

            X509Certificate2? certificateEx = null;
            try
            {
                if (certificate.Handle != IntPtr.Zero)
                {
                    certificateEx = new X509Certificate2(certificate);
                }
            }
            catch (SecurityException) { }
            catch (CryptographicException) { }

            return certificateEx;
        }

        //
        // Get certificate_authorities list, according to RFC 5246, Section 7.4.4.
        // Used only by client SSL code, never returns null.
        //
        private string[] GetRequestCertificateAuthorities()
        {
            string[] issuers = Array.Empty<string>();

            if (IsValidContext)
            {
                issuers = CertificateValidationPal.GetRequestCertificateAuthorities(_securityContext!);
            }
            return issuers;
        }

        /*++
            AcquireCredentials - Attempts to find Client Credential
            Information, that can be sent to the server.  In our case,
            this is only Client Certificates, that we have Credential Info.

            How it works:
                case 0: Cert Selection delegate is present
                        Always use its result as the client cert answer.
                        Try to use cached credential handle whenever feasible.
                        Do not use cached anonymous creds if the delegate has returned null
                        and the collection is not empty (allow responding with the cert later).

                case 1: Certs collection is empty
                        Always use the same statically acquired anonymous SSL Credential

                case 2: Before our Connection with the Server
                        If we have a cached credential handle keyed by first X509Certificate
                        **content** in the passed collection, then we use that cached
                        credential and hoping to restart a session.

                        Otherwise create a new anonymous (allow responding with the cert later).

                case 3: After our Connection with the Server (i.e. during handshake or re-handshake)
                        The server has requested that we send it a Certificate then
                        we Enumerate a list of server sent Issuers trying to match against
                        our list of Certificates, the first match is sent to the server.

                        Once we got a cert we again try to match cached credential handle if possible.
                        This will not restart a session but helps minimizing the number of handles we create.

                In the case of an error getting a Certificate or checking its private Key we fall back
                to the behavior of having no certs, case 1.

            Returns: True if cached creds were used, false otherwise.

        --*/

        private bool AcquireClientCredentials(ref byte[]? thumbPrint)
        {
            // Acquire possible Client Certificate information and set it on the handle.
            X509Certificate? clientCertificate = null;        // This is a candidate that can come from the user callback or be guessed when targeting a session restart.
            List<X509Certificate>? filteredCerts = null;      // This is an intermediate client certs collection that try to use if no selectedCert is available yet.
            string[] issuers;                                // This is a list of issuers sent by the server, only valid is we do know what the server cert is.

            bool sessionRestartAttempt = false; // If true and no cached creds we will use anonymous creds.

            if (_sslAuthenticationOptions.CertSelectionDelegate != null)
            {
                issuers = GetRequestCertificateAuthorities();

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, "Calling CertificateSelectionCallback");

                X509Certificate2? remoteCert = null;
                try
                {
                    remoteCert = CertificateValidationPal.GetRemoteCertificate(_securityContext!);
                    if (_sslAuthenticationOptions.ClientCertificates == null)
                    {
                        _sslAuthenticationOptions.ClientCertificates = new X509CertificateCollection();
                    }
                    clientCertificate = _sslAuthenticationOptions.CertSelectionDelegate(_sslAuthenticationOptions.TargetHost!, _sslAuthenticationOptions.ClientCertificates, remoteCert, issuers);
                }
                finally
                {
                    remoteCert?.Dispose();
                }


                if (clientCertificate != null)
                {
                    if (_credentialsHandle == null)
                    {
                        sessionRestartAttempt = true;
                    }

                    EnsureInitialized(ref filteredCerts).Add(clientCertificate);
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Log.CertificateFromDelegate(this);
                }
                else
                {
                    if (_sslAuthenticationOptions.ClientCertificates == null || _sslAuthenticationOptions.ClientCertificates.Count == 0)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.NoDelegateNoClientCert(this);

                        sessionRestartAttempt = true;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.NoDelegateButClientCert(this);
                    }
                }
            }
            else if (_credentialsHandle == null && _sslAuthenticationOptions.ClientCertificates != null && _sslAuthenticationOptions.ClientCertificates.Count > 0)
            {
                // This is where we attempt to restart a session by picking the FIRST cert from the collection.
                // Otherwise it is either server sending a client cert request or the session is renegotiated.
                clientCertificate = _sslAuthenticationOptions.ClientCertificates[0];
                sessionRestartAttempt = true;
                if (clientCertificate != null)
                {
                    EnsureInitialized(ref filteredCerts).Add(clientCertificate);
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Log.AttemptingRestartUsingCert(clientCertificate, this);
            }
            else if (_sslAuthenticationOptions.ClientCertificates != null && _sslAuthenticationOptions.ClientCertificates.Count > 0)
            {
                //
                // This should be a server request for the client cert sent over currently anonymous sessions.
                //
                issuers = GetRequestCertificateAuthorities();

                if (NetEventSource.Log.IsEnabled())
                {
                    if (issuers == null || issuers.Length == 0)
                    {
                        NetEventSource.Log.NoIssuersTryAllCerts(this);
                    }
                    else
                    {
                        NetEventSource.Log.LookForMatchingCerts(issuers.Length, this);
                    }
                }

                for (int i = 0; i < _sslAuthenticationOptions.ClientCertificates.Count; ++i)
                {
                    //
                    // Make sure we add only if the cert matches one of the issuers.
                    // If no issuers were sent and then try all client certs starting with the first one.
                    //
                    if (issuers != null && issuers.Length != 0)
                    {
                        X509Certificate2? certificateEx = null;
                        X509Chain? chain = null;
                        try
                        {
                            certificateEx = MakeEx(_sslAuthenticationOptions.ClientCertificates[i]);
                            if (certificateEx == null)
                            {
                                continue;
                            }

                            if (NetEventSource.Log.IsEnabled())
                                NetEventSource.Info(this, $"Root cert: {certificateEx}");

                            chain = new X509Chain();

                            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidName;
                            chain.Build(certificateEx);
                            bool found = false;

                            //
                            // We ignore any errors happened with chain.
                            //
                            if (chain.ChainElements.Count > 0)
                            {
                                int elementsCount = chain.ChainElements.Count;
                                for (int ii = 0; ii < elementsCount; ++ii)
                                {
                                    string issuer = chain.ChainElements[ii].Certificate!.Issuer;
                                    found = Array.IndexOf(issuers, issuer) != -1;
                                    if (found)
                                    {
                                        if (NetEventSource.Log.IsEnabled())
                                            NetEventSource.Info(this, $"Matched {issuer}");
                                        break;
                                    }
                                    if (NetEventSource.Log.IsEnabled())
                                        NetEventSource.Info(this, $"No match: {issuer}");
                                }
                            }

                            if (!found)
                            {
                                continue;
                            }
                        }
                        finally
                        {
                            if (chain != null)
                            {
                                chain.Dispose();

                                int elementsCount = chain.ChainElements.Count;
                                for (int element = 0; element < elementsCount; element++)
                                {
                                    chain.ChainElements[element].Certificate!.Dispose();
                                }
                            }

                            if (certificateEx != null && (object)certificateEx != (object)_sslAuthenticationOptions.ClientCertificates[i])
                            {
                                certificateEx.Dispose();
                            }
                        }
                    }

                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Log.SelectedCert(_sslAuthenticationOptions.ClientCertificates[i], this);

                    EnsureInitialized(ref filteredCerts).Add(_sslAuthenticationOptions.ClientCertificates[i]);
                }
            }

            bool cachedCred = false;                   // This is a return result from this method.
            X509Certificate2? selectedCert = null;     // This is a final selected cert (ensured that it does have private key with it).

            clientCertificate = null;

            if (NetEventSource.Log.IsEnabled())
            {
                if (filteredCerts != null && filteredCerts.Count != 0)
                {
                    NetEventSource.Log.CertsAfterFiltering(filteredCerts.Count, this);
                    NetEventSource.Log.FindingMatchingCerts(this);
                }
                else
                {
                    NetEventSource.Log.CertsAfterFiltering(0, this);
                    NetEventSource.Info(this, "No client certificate to choose from");
                }
            }

            //
            // ATTN: When the client cert was returned by the user callback OR it was guessed AND it has no private key,
            //       THEN anonymous (no client cert) credential will be used.
            //
            // SECURITY: Accessing X509 cert Credential is disabled for semitrust.
            // We no longer need to demand for unmanaged code permissions.
            // FindCertificateWithPrivateKey should do the right demand for us.
            if (filteredCerts != null)
            {
                for (int i = 0; i < filteredCerts.Count; ++i)
                {
                    clientCertificate = filteredCerts[i];
                    if ((selectedCert = FindCertificateWithPrivateKey(this, _sslAuthenticationOptions.IsServer, clientCertificate)) != null)
                    {
                        break;
                    }

                    clientCertificate = null;
                    selectedCert = null;
                }
            }

            Debug.Assert((object?)clientCertificate == (object?)selectedCert || clientCertificate!.Equals(selectedCert), "'selectedCert' does not match 'clientCertificate'.");

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Selected cert = {selectedCert}");

            try
            {
                // Try to locate cached creds first.
                //
                // SECURITY: selectedCert ref if not null is a safe object that does not depend on possible **user** inherited X509Certificate type.
                //
                byte[]? guessedThumbPrint = selectedCert?.GetCertHash();
                SafeFreeCredentials? cachedCredentialHandle = SslSessionsCache.TryCachedCredential(guessedThumbPrint, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);

                // We can probably do some optimization here. If the selectedCert is returned by the delegate
                // we can always go ahead and use the certificate to create our credential
                // (instead of going anonymous as we do here).
                if (sessionRestartAttempt &&
                    cachedCredentialHandle == null &&
                    selectedCert != null &&
                    SslStreamPal.StartMutualAuthAsAnonymous)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Info(this, "Reset to anonymous session.");

                    // IIS does not renegotiate a restarted session if client cert is needed.
                    // So we don't want to reuse **anonymous** cached credential for a new SSL connection if the client has passed some certificate.
                    // The following block happens if client did specify a certificate but no cached creds were found in the cache.
                    // Since we don't restart a session the server side can still challenge for a client cert.
                    if ((object?)clientCertificate != (object?)selectedCert)
                    {
                        selectedCert.Dispose();
                    }

                    guessedThumbPrint = null;
                    selectedCert = null;
                    clientCertificate = null;
                }

                if (cachedCredentialHandle != null)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Log.UsingCachedCredential(this);
                    _credentialsHandle = cachedCredentialHandle;
                    _selectedClientCertificate = clientCertificate;
                    cachedCred = true;
                    if (selectedCert != null)
                    {
                        _sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(selectedCert!);
                    }
                }
                else
                {
                    if (selectedCert != null)
                    {
                        _sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(selectedCert!);
                    }

                    _credentialsHandle = SslStreamPal.AcquireCredentialsHandle(_sslAuthenticationOptions.CertificateContext,
                            _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.EncryptionPolicy, _sslAuthenticationOptions.IsServer);

                    thumbPrint = guessedThumbPrint; // Delay until here in case something above threw.
                    _selectedClientCertificate = clientCertificate;
                }
            }
            finally
            {
                if (selectedCert != null && _sslAuthenticationOptions.CertificateContext != null)
                {
                    _sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(selectedCert);
                }
            }

            return cachedCred;
        }

        private static List<T> EnsureInitialized<T>(ref List<T>? list) => list ?? (list = new List<T>());

        //
        // Acquire Server Side Certificate information and set it on the class.
        //
        private bool AcquireServerCredentials(ref byte[]? thumbPrint)
        {
            X509Certificate? localCertificate = null;
            X509Certificate2? selectedCert = null;
            bool cachedCred = false;

            // There are three options for selecting the server certificate. When
            // selecting which to use, we prioritize the new ServerCertSelectionDelegate
            // API. If the new API isn't used we call LocalCertSelectionCallback (for compat
            // with .NET Framework), and if neither is set we fall back to using CertificateContext.
            if (_sslAuthenticationOptions.ServerCertSelectionDelegate != null)
            {
                localCertificate = _sslAuthenticationOptions.ServerCertSelectionDelegate(_sslAuthenticationOptions.TargetHost);
                if (localCertificate == null)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(this, $"ServerCertSelectionDelegate returned no certificaete for '{_sslAuthenticationOptions.TargetHost}'.");
                    throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, "ServerCertSelectionDelegate selected Cert");
            }
            else if (_sslAuthenticationOptions.CertSelectionDelegate != null)
            {
                X509CertificateCollection tempCollection = new X509CertificateCollection();
                tempCollection.Add(_sslAuthenticationOptions.CertificateContext!.Certificate!);
                // We pass string.Empty here to maintain strict compatibility with .NET Framework.
                localCertificate = _sslAuthenticationOptions.CertSelectionDelegate(string.Empty, tempCollection, null, Array.Empty<string>());
                if (localCertificate == null)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(this, $"CertSelectionDelegate returned no certificaete for '{_sslAuthenticationOptions.TargetHost}'.");
                    throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, "CertSelectionDelegate selected Cert");
            }
            else if (_sslAuthenticationOptions.CertificateContext != null)
            {
                selectedCert = _sslAuthenticationOptions.CertificateContext.Certificate;
            }

            if (selectedCert == null)
            {
                // We will get here if certificate was selected via legacy callback using X509Certificate
                // Fail immediately if no certificate was given.
                if (localCertificate == null)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(this, "Certiticate callback returned no certificaete.");
                    throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
                }

                // SECURITY: Accessing X509 cert Credential is disabled for semitrust.
                // We no longer need to demand for unmanaged code permissions.
                // EnsurePrivateKey should do the right demand for us.
                selectedCert = FindCertificateWithPrivateKey(this, _sslAuthenticationOptions.IsServer, localCertificate);

                if (selectedCert == null)
                {
                    throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
                }

                Debug.Assert(localCertificate.Equals(selectedCert), "'selectedCert' does not match 'localCertificate'.");
                _sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(selectedCert);
            }

            //
            // Note selectedCert is a safe ref possibly cloned from the user passed Cert object
            //
            byte[] guessedThumbPrint = selectedCert.GetCertHash();
            SafeFreeCredentials? cachedCredentialHandle = SslSessionsCache.TryCachedCredential(guessedThumbPrint, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);

            if (cachedCredentialHandle != null)
            {
                _credentialsHandle = cachedCredentialHandle;
                cachedCred = true;
            }
            else
            {
                _credentialsHandle = SslStreamPal.AcquireCredentialsHandle(_sslAuthenticationOptions.CertificateContext, _sslAuthenticationOptions.EnabledSslProtocols,
                        _sslAuthenticationOptions.EncryptionPolicy, _sslAuthenticationOptions.IsServer);
                thumbPrint = guessedThumbPrint;
            }

            return cachedCred;
        }

        //
        internal ProtocolToken NextMessage(ReadOnlySpan<byte> incomingBuffer)
        {
            byte[]? nextmsg = null;
            SecurityStatusPal status = GenerateToken(incomingBuffer, ref nextmsg);

            if (!_sslAuthenticationOptions.IsServer && status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, "NextMessage() returned SecurityStatusPal.CredentialsNeeded");

                SetRefreshCredentialNeeded();
                status = GenerateToken(incomingBuffer, ref nextmsg);
            }

            ProtocolToken token = new ProtocolToken(nextmsg, status);

            if (NetEventSource.Log.IsEnabled())
            {
                if (token.Failed)
                {
                    NetEventSource.Error(this, $"Authentication failed. Status: {status.ToString()}, Exception message: {token.GetException()!.Message}");
                }
            }
            return token;
        }

        /*++
            GenerateToken - Called after each successive state
            in the Client - Server handshake.  This function
            generates a set of bytes that will be sent next to
            the server.  The server responds, each response,
            is pass then into this function, again, and the cycle
            repeats until successful connection, or failure.

            Input:
                input  - bytes from the wire
                output - ref to byte [], what we will send to the
                    server in response
            Return:
                status - error information
        --*/
        private SecurityStatusPal GenerateToken(ReadOnlySpan<byte> inputBuffer, ref byte[]? output)
        {
            byte[]? result = Array.Empty<byte>();
            SecurityStatusPal status = default;
            bool cachedCreds = false;
            byte[]? thumbPrint = null;

            //
            // Looping through ASC or ISC with potentially cached credential that could have been
            // already disposed from a different thread before ISC or ASC dir increment a cred ref count.
            //
            try
            {
                do
                {
                    thumbPrint = null;
                    if (_refreshCredentialNeeded)
                    {
                        cachedCreds = _sslAuthenticationOptions.IsServer
                                        ? AcquireServerCredentials(ref thumbPrint)
                                        : AcquireClientCredentials(ref thumbPrint);
                    }

                    if (_sslAuthenticationOptions.IsServer)
                    {
                        status = SslStreamPal.AcceptSecurityContext(
                                      ref _credentialsHandle!,
                                      ref _securityContext,
                                      inputBuffer,
                                      ref result,
                                      _sslAuthenticationOptions);
                    }
                    else
                    {
                        status = SslStreamPal.InitializeSecurityContext(
                                       ref _credentialsHandle!,
                                       ref _securityContext,
                                       _sslAuthenticationOptions.TargetHost,
                                       inputBuffer,
                                       ref result,
                                       _sslAuthenticationOptions);
                    }
                } while (cachedCreds && _credentialsHandle == null);
            }
            finally
            {
                if (_refreshCredentialNeeded)
                {
                    _refreshCredentialNeeded = false;

                    //
                    // Assuming the ISC or ASC has referenced the credential,
                    // we want to call dispose so to decrement the effective ref count.
                    //
                    _credentialsHandle?.Dispose();

                    //
                    // This call may bump up the credential reference count further.
                    // Note that thumbPrint is retrieved from a safe cert object that was possible cloned from the user passed cert.
                    //
                    if (!cachedCreds && _securityContext != null && !_securityContext.IsInvalid && _credentialsHandle != null && !_credentialsHandle.IsInvalid)
                    {
                        SslSessionsCache.CacheCredential(_credentialsHandle, thumbPrint, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);
                    }
                }
            }

            output = result;

            return status;
        }

        internal SecurityStatusPal Renegotiate(out byte[]? output)
        {
            return SslStreamPal.Renegotiate(
                                      ref _credentialsHandle!,
                                      ref _securityContext,
                                      _sslAuthenticationOptions,
                                      out output);
        }

        /*++
            ProcessHandshakeSuccess -
               Called on successful completion of Handshake -
               used to set header/trailer sizes for encryption use

            Fills in the information about established protocol
        --*/
        internal void ProcessHandshakeSuccess()
        {
            if (_negotiatedApplicationProtocol == default)
            {
                // try to get ALPN info unless we already have it. (renegotiation)
                byte[]? alpnResult = SslStreamPal.GetNegotiatedApplicationProtocol(_securityContext!);
                _negotiatedApplicationProtocol = alpnResult == null ? default : new SslApplicationProtocol(alpnResult, false);
            }

            SslStreamPal.QueryContextStreamSizes(_securityContext!, out StreamSizes streamSizes);

            _headerSize = streamSizes.Header;
            _trailerSize = streamSizes.Trailer;
            _maxDataSize = checked(streamSizes.MaximumMessage - (_headerSize + _trailerSize));
            Debug.Assert(_maxDataSize > 0, "_maxDataSize > 0");

            SslStreamPal.QueryContextConnectionInfo(_securityContext!, out _connectionInfo);
        }

        /*++
            Encrypt - Encrypts our bytes before we send them over the wire

            PERF: make more efficient, this does an extra copy when the offset
            is non-zero.

            Input:
                buffer - bytes for sending
                offset -
                size   -
                output - Encrypted bytes
        --*/
        internal SecurityStatusPal Encrypt(ReadOnlyMemory<byte> buffer, ref byte[] output, out int resultSize)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.DumpBuffer(this, buffer.Span);

            byte[] writeBuffer = output;

            SecurityStatusPal secStatus = SslStreamPal.EncryptMessage(
                _securityContext!,
                buffer,
                _headerSize,
                _trailerSize,
                ref writeBuffer,
                out resultSize);

            if (secStatus.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"ERROR {secStatus}");
            }
            else
            {
                output = writeBuffer;
            }

            return secStatus;
        }

        internal SecurityStatusPal Decrypt(Span<byte> buffer, out int outputOffset, out int outputCount)
        {
            SecurityStatusPal status = SslStreamPal.DecryptMessage(_securityContext!, buffer, out outputOffset, out outputCount);
            if (NetEventSource.Log.IsEnabled() && status.ErrorCode == SecurityStatusPalErrorCode.OK)
            {
                NetEventSource.DumpBuffer(this, buffer.Slice(outputOffset, outputCount));
            }

            return status;
        }

        /*++
            VerifyRemoteCertificate - Validates the content of a Remote Certificate

            checkCRL if true, checks the certificate revocation list for validity.
            checkCertName, if true checks the CN field of the certificate
        --*/

        //This method validates a remote certificate.
        internal bool VerifyRemoteCertificate(RemoteCertificateValidationCallback? remoteCertValidationCallback, ref ProtocolToken? alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus)
        {
            sslPolicyErrors = SslPolicyErrors.None;
            chainStatus = X509ChainStatusFlags.NoError;

            // We don't catch exceptions in this method, so it's safe for "accepted" be initialized with true.
            bool success = false;
            X509Chain? chain = null;
            X509Certificate2Collection? remoteCertificateStore = null;

            try
            {
                X509Certificate2? certificate = CertificateValidationPal.GetRemoteCertificate(_securityContext, out remoteCertificateStore);

                if (_remoteCertificate != null && certificate != null &&
                    certificate.RawData.AsSpan().SequenceEqual(_remoteCertificate.RawData))
                {
                    // This is renegotiation or TLS 1.3 and the certificate did not change.
                    // There is no reason to process callback again as we already established trust.
                    return true;
                }

                _remoteCertificate = certificate;

                if (_remoteCertificate == null)
                {
                    if (NetEventSource.Log.IsEnabled() && RemoteCertRequired) NetEventSource.Error(this, $"Remote certificate required, but no remote certificate received");
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                }
                else
                {
                    chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = _sslAuthenticationOptions.CertificateRevocationCheckMode;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                    // Authenticate the remote party: (e.g. when operating in server mode, authenticate the client).
                    chain.ChainPolicy.ApplicationPolicy.Add(_sslAuthenticationOptions.IsServer ? s_clientAuthOid : s_serverAuthOid);

                    if (remoteCertificateStore != null)
                    {
                        chain.ChainPolicy.ExtraStore.AddRange(remoteCertificateStore);
                    }

                    sslPolicyErrors |= CertificateValidationPal.VerifyCertificateProperties(
                        _securityContext!,
                        chain,
                        _remoteCertificate,
                        _sslAuthenticationOptions.CheckCertName,
                        _sslAuthenticationOptions.IsServer,
                        _sslAuthenticationOptions.TargetHost);
                }

                if (remoteCertValidationCallback != null)
                {
                    object? sender = _ssl;
                    if (sender == null)
                    {
                        throw new ObjectDisposedException(nameof(SslStream));
                    }

                    success = remoteCertValidationCallback(sender, _remoteCertificate, chain, sslPolicyErrors);
                }
                else
                {
                    if (!RemoteCertRequired)
                    {
                        sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
                    }

                    success = (sslPolicyErrors == SslPolicyErrors.None);
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    LogCertificateValidation(remoteCertValidationCallback, sslPolicyErrors, success, chain!);
                    NetEventSource.Info(this, $"Cert validation, remote cert = {_remoteCertificate}");
                }

                if (!success)
                {
                    alertToken = CreateFatalHandshakeAlertToken(sslPolicyErrors, chain!);
                    if (chain != null)
                    {
                        foreach (X509ChainStatus status in chain.ChainStatus)
                        {
                            chainStatus |= status.Status;
                        }
                    }
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
                        chain.ChainElements[i].Certificate!.Dispose();
                    }

                    chain.Dispose();
                }

                if (remoteCertificateStore != null)
                {
                    int certCount = remoteCertificateStore.Count;
                    for (int i = 0; i < certCount; i++)
                    {
                        remoteCertificateStore[i].Dispose();
                    }
                }
            }

            return success;
        }

        public ProtocolToken? CreateFatalHandshakeAlertToken(SslPolicyErrors sslPolicyErrors, X509Chain chain)
        {
            TlsAlertMessage alertMessage;

            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    alertMessage = GetAlertMessageFromChain(chain);
                    break;
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    alertMessage = TlsAlertMessage.BadCertificate;
                    break;
                case SslPolicyErrors.RemoteCertificateNotAvailable:
                default:
                    alertMessage = TlsAlertMessage.CertificateUnknown;
                    break;
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(this, $"alertMessage:{alertMessage}");

            SecurityStatusPal status;
            status = SslStreamPal.ApplyAlertToken(ref _credentialsHandle, _securityContext, TlsAlertType.Fatal, alertMessage);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, $"ApplyAlertToken() returned {status.ErrorCode}");

                if (status.Exception != null)
                {
                    ExceptionDispatchInfo.Throw(status.Exception);
                }

                return null;
            }

            return GenerateAlertToken();
        }

        public ProtocolToken? CreateShutdownToken()
        {
            SecurityStatusPal status;
            status = SslStreamPal.ApplyShutdownToken(ref _credentialsHandle, _securityContext!);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(this, $"ApplyAlertToken() returned {status.ErrorCode}");

                if (status.Exception != null)
                {
                    ExceptionDispatchInfo.Throw(status.Exception);
                }

                return null;
            }

            return GenerateAlertToken();
        }

        private ProtocolToken GenerateAlertToken()
        {
            byte[]? nextmsg = null;

            SecurityStatusPal status;
            status = GenerateToken(default, ref nextmsg);

            return new ProtocolToken(nextmsg, status);
        }

        private static TlsAlertMessage GetAlertMessageFromChain(X509Chain chain)
        {
            foreach (X509ChainStatus chainStatus in chain.ChainStatus)
            {
                if (chainStatus.Status == X509ChainStatusFlags.NoError)
                {
                    continue;
                }

                if ((chainStatus.Status &
                    (X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.PartialChain |
                     X509ChainStatusFlags.Cyclic)) != 0)
                {
                    return TlsAlertMessage.UnknownCA;
                }

                if ((chainStatus.Status &
                    (X509ChainStatusFlags.Revoked | X509ChainStatusFlags.OfflineRevocation)) != 0)
                {
                    return TlsAlertMessage.CertificateRevoked;
                }

                if ((chainStatus.Status &
                    (X509ChainStatusFlags.CtlNotTimeValid | X509ChainStatusFlags.NotTimeNested |
                     X509ChainStatusFlags.NotTimeValid)) != 0)
                {
                    return TlsAlertMessage.CertificateExpired;
                }

                if ((chainStatus.Status & X509ChainStatusFlags.CtlNotValidForUsage) != 0)
                {
                    return TlsAlertMessage.UnsupportedCert;
                }

                if ((chainStatus.Status &
                    (X509ChainStatusFlags.CtlNotSignatureValid | X509ChainStatusFlags.InvalidExtension |
                     X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.InvalidPolicyConstraints) |
                     X509ChainStatusFlags.NoIssuanceChainPolicy | X509ChainStatusFlags.NotValidForUsage) != 0)
                {
                    return TlsAlertMessage.BadCertificate;
                }

                // All other errors:
                return TlsAlertMessage.CertificateUnknown;
            }

            return TlsAlertMessage.BadCertificate;
        }

        private void LogCertificateValidation(RemoteCertificateValidationCallback? remoteCertValidationCallback, SslPolicyErrors sslPolicyErrors, bool success, X509Chain chain)
        {
            if (!NetEventSource.Log.IsEnabled())
                return;

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                NetEventSource.Log.RemoteCertificateError(this, SR.net_log_remote_cert_has_errors);
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                {
                    NetEventSource.Log.RemoteCertificateError(this, SR.net_log_remote_cert_not_available);
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    NetEventSource.Log.RemoteCertificateError(this, SR.net_log_remote_cert_name_mismatch);
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    string chainStatusString = "ChainStatus: ";
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        chainStatusString += "\t" + chainStatus.StatusInformation;
                    }
                    NetEventSource.Log.RemoteCertificateError(this, chainStatusString);
                }
            }

            if (success)
            {
                if (remoteCertValidationCallback != null)
                {
                    NetEventSource.Log.RemoteCertDeclaredValid(this);
                }
                else
                {
                    NetEventSource.Log.RemoteCertHasNoErrors(this);
                }
            }
            else
            {
                if (remoteCertValidationCallback != null)
                {
                    NetEventSource.Log.RemoteCertUserDeclaredInvalid(this);
                }
            }
        }
    }

    // ProtocolToken - used to process and handle the return codes from the SSPI wrapper
    internal sealed class ProtocolToken
    {
        internal SecurityStatusPal Status;
        internal byte[]? Payload;
        internal int Size;

        internal bool Failed
        {
            get
            {
                return ((Status.ErrorCode != SecurityStatusPalErrorCode.OK) && (Status.ErrorCode != SecurityStatusPalErrorCode.ContinueNeeded));
            }
        }

        internal bool Done
        {
            get
            {
                return (Status.ErrorCode == SecurityStatusPalErrorCode.OK);
            }
        }

        internal bool Renegotiate
        {
            get
            {
                return (Status.ErrorCode == SecurityStatusPalErrorCode.Renegotiate);
            }
        }

        internal bool CloseConnection
        {
            get
            {
                return (Status.ErrorCode == SecurityStatusPalErrorCode.ContextExpired);
            }
        }

        internal ProtocolToken(byte[]? data, SecurityStatusPal status)
        {
            Status = status;
            Payload = data;
            Size = data != null ? data.Length : 0;
        }

        internal Exception? GetException()
        {
            // If it's not done, then there's got to be an error, even if it's
            // a Handshake message up, and we only have a Warning message.
            return Done ? null : SslStreamPal.GetException(Status);
        }
    }
}
