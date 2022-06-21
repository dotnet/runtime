// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    /// <summary>
    /// Represents a stateful authentication exchange that uses the Negotiate, NTLM or Kerberos security protocols
    /// to authenticate the client or server, in client-server communication.
    /// </summary>
    public sealed class NegotiateAuthentication : IDisposable
    {
        private readonly NTAuthentication? _ntAuthentication;
        private readonly string _requestedPackage;
        private readonly bool _isServer;
        private IIdentity? _remoteIdentity;
        private TokenImpersonationLevel _requiredImpersonationLevel;
        private ProtectionLevel _requiredProtectionLevel;
        private ExtendedProtectionPolicy? _extendedProtectionPolicy;
        private bool _isSecureConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiateAuthentication"/>
        /// for client-side authentication session.
        /// </summary>
        /// <param name="clientOptions">The property bag for the authentication options.</param>
        public NegotiateAuthentication(NegotiateAuthenticationClientOptions clientOptions)
        {
            ArgumentNullException.ThrowIfNull(clientOptions);

            ContextFlagsPal contextFlags = ContextFlagsPal.Connection;

            contextFlags |= clientOptions.RequiredProtectionLevel switch
            {
                ProtectionLevel.Sign => ContextFlagsPal.InitIntegrity,
                ProtectionLevel.EncryptAndSign => ContextFlagsPal.InitIntegrity | ContextFlagsPal.Confidentiality,
                _ => 0
            };

            contextFlags |= clientOptions.RequireMutualAuthentication ? ContextFlagsPal.MutualAuth : 0;

            contextFlags |= clientOptions.AllowedImpersonationLevel switch
            {
                TokenImpersonationLevel.Identification => ContextFlagsPal.InitIdentify,
                TokenImpersonationLevel.Delegation => ContextFlagsPal.Delegate,
                _ => 0
            };

            _isServer = false;
            _requestedPackage = clientOptions.Package;
            _requiredImpersonationLevel = TokenImpersonationLevel.None;
            _requiredProtectionLevel = clientOptions.RequiredProtectionLevel;
            try
            {
                _ntAuthentication = new NTAuthentication(
                    isServer: false,
                    clientOptions.Package,
                    clientOptions.Credential,
                    clientOptions.TargetName,
                    contextFlags,
                    clientOptions.Binding);
            }
            catch (PlatformNotSupportedException) // Managed implementation, Unix
            {
            }
            catch (NotSupportedException) // Windows implementation
            {
            }
            catch (Win32Exception) // Unix implementation in native layer
            {
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiateAuthentication"/>
        /// for server-side authentication session.
        /// </summary>
        /// <param name="serverOptions">The property bag for the authentication options.</param>
        public NegotiateAuthentication(NegotiateAuthenticationServerOptions serverOptions)
        {
            ArgumentNullException.ThrowIfNull(serverOptions);

            ContextFlagsPal contextFlags = serverOptions.RequiredProtectionLevel switch
            {
                ProtectionLevel.Sign => ContextFlagsPal.AcceptIntegrity,
                ProtectionLevel.EncryptAndSign => ContextFlagsPal.AcceptIntegrity | ContextFlagsPal.Confidentiality,
                _ => 0
            } | ContextFlagsPal.Connection;

            if (serverOptions.Policy is not null)
            {
                if (serverOptions.Policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    contextFlags |= ContextFlagsPal.AllowMissingBindings;
                }

                if (serverOptions.Policy.PolicyEnforcement != PolicyEnforcement.Never &&
                    serverOptions.Policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
                {
                    contextFlags |= ContextFlagsPal.ProxyBindings;
                }
            }

            _isServer = true;
            _requestedPackage = serverOptions.Package;
            _requiredImpersonationLevel = serverOptions.RequiredImpersonationLevel;
            _requiredProtectionLevel = serverOptions.RequiredProtectionLevel;
            _extendedProtectionPolicy = serverOptions.Policy;
            _isSecureConnection = serverOptions.Binding != null;
            try
            {
                _ntAuthentication = new NTAuthentication(
                    isServer: true,
                    serverOptions.Package,
                    serverOptions.Credential,
                    null,
                    contextFlags,
                    serverOptions.Binding);
            }
            catch (PlatformNotSupportedException) // Managed implementation, Unix
            {
            }
            catch (NotSupportedException) // Windows implementation
            {
            }
            catch (Win32Exception) // Unix implementation in native layer
            {
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="NegotiateAuthentication"/>
        /// and optionally releases the managed resources.
        /// </summary>
        public void Dispose()
        {
            _ntAuthentication?.CloseContext();
            if (_remoteIdentity is IDisposable disposableRemoteIdentity)
            {
                disposableRemoteIdentity.Dispose();
            }
        }

        /// <summary>
        /// Indicates whether authentication was successfully completed and the session
        /// was established.
        /// </summary>
        public bool IsAuthenticated => _ntAuthentication?.IsCompleted ?? false;

        /// <summary>
        /// Indicates the negotiated level of protection.
        /// </summary>
        /// <remarks>
        /// The negotiated level of protection is only available when the session
        /// authentication was finished (see <see cref="IsAuthenticated" />). The
        /// protection level can be higher than the initially requested protection
        /// level specified by <see cref="NegotiateAuthenticationClientOptions.RequiredProtectionLevel" /> or
        /// <see cref="NegotiateAuthenticationServerOptions.RequiredProtectionLevel" />.
        /// </remarks>
        public ProtectionLevel ProtectionLevel =>
            !IsSigned ? ProtectionLevel.None :
            !IsEncrypted ? ProtectionLevel.Sign :
            ProtectionLevel.EncryptAndSign;

        /// <summary>
        /// Indicates whether data signing was negotiated.
        /// </summary>
        public bool IsSigned => _ntAuthentication?.IsIntegrityFlag ?? false;

        /// <summary>
        /// Indicates whether data encryption was negotiated.
        /// </summary>
        public bool IsEncrypted => _ntAuthentication?.IsConfidentialityFlag ?? false;

        /// <summary>
        /// Indicates whether both server and client have been authenticated.
        /// </summary>
        public bool IsMutuallyAuthenticated => _ntAuthentication?.IsMutualAuthFlag ?? false;

        /// <summary>
        /// Indicates whether the local side of the authentication is representing
        /// the server.
        /// </summary>
        public bool IsServer => _isServer;

        /// <summary>
        /// Name of the negotiated authentication package.
        /// </summary>
        /// <remarks>
        /// The negotiated authentication package is only available when the session
        /// authentication was finished (see <see cref="IsAuthenticated" />). For
        /// unfinished authentication sessions the value is undefined and usually
        /// returns the initial authentication package name specified in
        /// <see cref="NegotiateAuthenticationClientOptions.Package" /> or
        /// <see cref="NegotiateAuthenticationServerOptions.Package" />.
        ///
        /// If the Negotiate package was used for authentication the value of this
        /// property will be Kerberos, NTLM, or any other specific protocol that was
        /// negotiated between both sides of the authentication.
        /// </remarks>
        public string Package => _ntAuthentication?.ProtocolName ?? _requestedPackage;

        /// <summary>
        /// Gets target name (service principal name) of the server.
        /// </summary>
        /// <remarks>
        /// For server-side of the authentication the property returns the target name
        /// specified by the client after successful authentication (see <see cref="IsAuthenticated" />).
        ///
        /// For client-side of the authentication the property returns the target name
        /// specified in <see cref="NegotiateAuthenticationClientOptions.TargetName" />.
        /// </remarks>
        public string? TargetName => IsServer ? _ntAuthentication?.ClientSpecifiedSpn : _ntAuthentication?.Spn;

        /// <summary>
        /// Gets information about the identity of the remote party.
        /// </summary>
        /// <returns>
        /// An <see cref="IIdentity" /> object that describes the identity of the remote endpoint.
        /// </returns>
        /// <exception cref="InvalidOperationException">Authentication failed or has not occurred.</exception>
        /// <exception cref="Win32Exception">System error occurred when trying to retrieve the identity.</exception>
        public IIdentity RemoteIdentity
        {
            get
            {
                IIdentity? identity = _remoteIdentity;
                if (identity is null)
                {
                    if (!IsAuthenticated || _ntAuthentication == null)
                    {
                        throw new InvalidOperationException(SR.net_auth_noauth);
                    }

                    if (IsServer)
                    {
                        Debug.Assert(!OperatingSystem.IsTvOS(), "Server authentication is not supported on tvOS");
                        _remoteIdentity = identity = NegotiateStreamPal.GetIdentity(_ntAuthentication);
                    }
                    else
                    {
                        return new GenericIdentity(TargetName ?? string.Empty, Package);
                    }
                }
                return identity;
            }
        }

        /// <summary>
        /// One of the <see cref="TokenImpersonationLevel" /> values, indicating the negotiated
        /// level of impresonation.
        /// </summary>
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel
        {
            get
            {
                // We should suppress the delegate flag in NTLM case.
                return
                    _ntAuthentication!.IsDelegationFlag && _ntAuthentication.ProtocolName != NegotiationInfoClass.NTLM ? TokenImpersonationLevel.Delegation :
                    _ntAuthentication.IsIdentifyFlag ? TokenImpersonationLevel.Identification :
                    TokenImpersonationLevel.Impersonation;
            }
        }

        /// <summary>
        /// Evaluates an authentication token sent by the other party and returns a token in response.
        /// </summary>
        /// <param name="incomingBlob">Incoming authentication token, or empty value when initiating the authentication exchange.</param>
        /// <param name="statusCode">Status code returned by the authentication provider.</param>
        /// <returns>Outgoing authentication token to be sent to the other party.</returns>
        /// <remarks>
        /// When initiating the authentication exchange, one of the parties starts
        /// with an empty incomingBlob parameter.
        ///
        /// Successful step of the authentication returns either <see cref="NegotiateAuthenticationStatusCode.Completed" />
        /// or <see cref="NegotiateAuthenticationStatusCode.ContinueNeeded" /> status codes.
        /// Any other status code indicates an unrecoverable error.
        ///
        /// When <see cref="NegotiateAuthenticationStatusCode.ContinueNeeded" /> is returned the
        /// return value is an authentication token to be transported to the other party.
        /// </remarks>
        public byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
        {
            if (_ntAuthentication == null)
            {
                // Unsupported protocol
                statusCode = NegotiateAuthenticationStatusCode.Unsupported;
                return null;
            }

            byte[]? blob = _ntAuthentication.GetOutgoingBlob(incomingBlob, false, out SecurityStatusPal securityStatus);

            // Map error codes
            statusCode = securityStatus.ErrorCode switch
            {
                SecurityStatusPalErrorCode.OK => NegotiateAuthenticationStatusCode.Completed,
                SecurityStatusPalErrorCode.ContinueNeeded => NegotiateAuthenticationStatusCode.ContinueNeeded,

                // These code should never be returned and they should be handled internally
                SecurityStatusPalErrorCode.CompleteNeeded => NegotiateAuthenticationStatusCode.Completed,
                SecurityStatusPalErrorCode.CompAndContinue => NegotiateAuthenticationStatusCode.ContinueNeeded,

                SecurityStatusPalErrorCode.ContextExpired => NegotiateAuthenticationStatusCode.ContextExpired,
                SecurityStatusPalErrorCode.Unsupported => NegotiateAuthenticationStatusCode.Unsupported,
                SecurityStatusPalErrorCode.PackageNotFound => NegotiateAuthenticationStatusCode.Unsupported,
                SecurityStatusPalErrorCode.CannotInstall => NegotiateAuthenticationStatusCode.Unsupported,
                SecurityStatusPalErrorCode.InvalidToken => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.QopNotSupported => NegotiateAuthenticationStatusCode.QopNotSupported,
                SecurityStatusPalErrorCode.NoImpersonation => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.LogonDenied => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.UnknownCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.NoCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.MessageAltered => NegotiateAuthenticationStatusCode.MessageAltered,
                SecurityStatusPalErrorCode.OutOfSequence => NegotiateAuthenticationStatusCode.OutOfSequence,
                SecurityStatusPalErrorCode.NoAuthenticatingAuthority => NegotiateAuthenticationStatusCode.InvalidCredentials,
                SecurityStatusPalErrorCode.IncompleteCredentials => NegotiateAuthenticationStatusCode.InvalidCredentials,
                SecurityStatusPalErrorCode.IllegalMessage => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.CertExpired => NegotiateAuthenticationStatusCode.CredentialsExpired,
                SecurityStatusPalErrorCode.SecurityQosFailed => NegotiateAuthenticationStatusCode.QopNotSupported,
                SecurityStatusPalErrorCode.UnsupportedPreauth => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.BadBinding => NegotiateAuthenticationStatusCode.BadBinding,
                SecurityStatusPalErrorCode.UntrustedRoot => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.SmartcardLogonRequired => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.WrongPrincipal => NegotiateAuthenticationStatusCode.UnknownCredentials,
                SecurityStatusPalErrorCode.CannotPack => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.TimeSkew => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.AlgorithmMismatch => NegotiateAuthenticationStatusCode.InvalidToken,
                SecurityStatusPalErrorCode.CertUnknown => NegotiateAuthenticationStatusCode.UnknownCredentials,

                // Processing partial inputs is not supported, so this is result of incorrect input
                SecurityStatusPalErrorCode.IncompleteMessage => NegotiateAuthenticationStatusCode.InvalidToken,

                _ => NegotiateAuthenticationStatusCode.GenericFailure,
            };

            // Additional policy validation
            if (statusCode == NegotiateAuthenticationStatusCode.Completed)
            {
                if (IsServer && _extendedProtectionPolicy != null && !CheckSpn())
                {
                    statusCode = NegotiateAuthenticationStatusCode.TargetUnknown;
                }
                else if (_requiredImpersonationLevel != TokenImpersonationLevel.None && ImpersonationLevel < _requiredImpersonationLevel)
                {
                    statusCode = NegotiateAuthenticationStatusCode.ImpersonationValidationFailed;
                }
                else if (_requiredProtectionLevel != ProtectionLevel.None && ProtectionLevel < _requiredProtectionLevel)
                {
                    statusCode = NegotiateAuthenticationStatusCode.SecurityQosFailed;
                }
            }

            return blob;
        }

        /// <summary>
        /// Evaluates an authentication token sent by the other party and returns a token in response.
        /// </summary>
        /// <param name="incomingBlob">Incoming authentication token, or empty value when initiating the authentication exchange. Encoded as base64.</param>
        /// <param name="statusCode">Status code returned by the authentication provider.</param>
        /// <returns>Outgoing authentication token to be sent to the other party, encoded as base64.</returns>
        /// <remarks>
        /// When initiating the authentication exchange, one of the parties starts
        /// with an empty incomingBlob parameter.
        ///
        /// Successful step of the authentication returns either <see cref="NegotiateAuthenticationStatusCode.Completed" />
        /// or <see cref="NegotiateAuthenticationStatusCode.ContinueNeeded" /> status codes.
        /// Any other status code indicates an unrecoverable error.
        ///
        /// When <see cref="NegotiateAuthenticationStatusCode.ContinueNeeded" /> is returned the
        /// return value is an authentication token to be transported to the other party.
        /// </remarks>
        public string? GetOutgoingBlob(string? incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
        {
            byte[]? decodedIncomingBlob = null;
            if (!string.IsNullOrEmpty(incomingBlob))
            {
                decodedIncomingBlob = Convert.FromBase64String(incomingBlob);
            }
            byte[]? decodedOutgoingBlob = GetOutgoingBlob(decodedIncomingBlob, out statusCode);

            string? outgoingBlob = null;
            if (decodedOutgoingBlob != null && decodedOutgoingBlob.Length > 0)
            {
                outgoingBlob = Convert.ToBase64String(decodedOutgoingBlob);
            }

            return outgoingBlob;
        }

        /// <summary>
        /// Wrap an input message with signature and optionally with an encryption.
        /// </summary>
        /// <param name="input">Input message to be wrapped.</param>
        /// <param name="outputWriter">Buffer writter where the wrapped message is written.</param>
        /// <param name="requestEncryption">Specifies whether encryption is requested.</param>
        /// <param name="isEncrypted">Specifies whether encryption was applied in the wrapping.</param>
        /// <returns>
        /// <see cref="NegotiateAuthenticationStatusCode.Completed" /> on success, other
        /// <see cref="NegotiateAuthenticationStatusCode" /> values on failure.
        /// </returns>
        /// <remarks>
        /// Like the <see href="https://datatracker.ietf.org/doc/html/rfc2743#page-65">GSS_Wrap</see> API
        /// the authentication protocol implementation may choose to override the requested value in the
        /// requestEncryption parameter. This may result in either downgrade or upgrade of the protection
        /// level.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Authentication failed or has not occurred.</exception>
        public NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
        {
            if (!IsAuthenticated || _ntAuthentication == null)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }

            return _ntAuthentication.Wrap(input, outputWriter, requestEncryption, out isEncrypted);
        }

        /// <summary>
        /// Unwrap an input message with signature or encryption applied by the other party.
        /// </summary>
        /// <param name="input">Input message to be unwrapped.</param>
        /// <param name="outputWriter">Buffer writter where the unwrapped message is written.</param>
        /// <param name="wasEncrypted">
        /// On output specifies whether the wrapped message had encryption applied.
        /// </param>
        /// <returns>
        /// <see cref="NegotiateAuthenticationStatusCode.Completed" /> on success.
        /// <see cref="NegotiateAuthenticationStatusCode.MessageAltered" /> if the message signature was
        /// invalid.
        /// <see cref="NegotiateAuthenticationStatusCode.InvalidToken" /> if the wrapped message was
        /// in invalid format.
        /// Other <see cref="NegotiateAuthenticationStatusCode" /> values on failure.
        /// </returns>
        /// <exception cref="InvalidOperationException">Authentication failed or has not occurred.</exception>
        public NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
        {
            if (!IsAuthenticated || _ntAuthentication == null)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }

            return _ntAuthentication.Unwrap(input, outputWriter, out wasEncrypted);
        }

        /// <summary>
        /// Unwrap an input message with signature or encryption applied by the other party.
        /// </summary>
        /// <param name="input">Input message to be unwrapped. On output contains the decoded data.</param>
        /// <param name="unwrappedOffset">Offset in the input buffer where the unwrapped message was written.</param>
        /// <param name="unwrappedLength">Length of the unwrapped message.</param>
        /// <param name="wasEncrypted">
        /// On output specifies whether the wrapped message had encryption applied.
        /// </param>
        /// <returns>
        /// <see cref="NegotiateAuthenticationStatusCode.Completed" /> on success.
        /// <see cref="NegotiateAuthenticationStatusCode.MessageAltered" /> if the message signature was
        /// invalid.
        /// <see cref="NegotiateAuthenticationStatusCode.InvalidToken" /> if the wrapped message was
        /// in invalid format.
        /// Other <see cref="NegotiateAuthenticationStatusCode" /> values on failure.
        /// </returns>
        /// <exception cref="InvalidOperationException">Authentication failed or has not occurred.</exception>
        public NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
        {
            if (!IsAuthenticated || _ntAuthentication == null)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }

            return _ntAuthentication.UnwrapInPlace(input, out unwrappedOffset, out unwrappedLength, out wasEncrypted);
        }

        private bool CheckSpn()
        {
            Debug.Assert(_ntAuthentication != null);
            Debug.Assert(_extendedProtectionPolicy != null);

            if (_ntAuthentication.IsKerberos)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_spn_kerberos);
                return true;
            }

            if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Never)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_spn_disabled);
                return true;
            }

            if (_isSecureConnection &&  _extendedProtectionPolicy.ProtectionScenario == ProtectionScenario.TransportSelected)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_spn_cbt);
                return true;
            }

            if (_extendedProtectionPolicy.CustomServiceNames == null)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_spns);
                return true;
            }

            string? clientSpn = _ntAuthentication.ClientSpecifiedSpn;

            if (string.IsNullOrEmpty(clientSpn))
            {
                if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_spn_whensupported);
                    return true;
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_spn_failed_always);
                    return false;
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_spn, clientSpn);
            bool found = _extendedProtectionPolicy.CustomServiceNames.Contains(clientSpn);

            if (NetEventSource.Log.IsEnabled())
            {
                if (found)
                {
                    NetEventSource.Info(this, SR.net_log_listener_spn_passed);
                }
                else
                {
                    NetEventSource.Info(this, SR.net_log_listener_spn_failed);

                    if (_extendedProtectionPolicy.CustomServiceNames.Count == 0)
                    {
                        NetEventSource.Info(this, SR.net_log_listener_spn_failed_empty);
                    }
                    else
                    {
                        NetEventSource.Info(this, SR.net_log_listener_spn_failed_dump);
                        foreach (string serviceName in _extendedProtectionPolicy.CustomServiceNames)
                        {
                            NetEventSource.Info(this, "\t" + serviceName);
                        }
                    }
                }
            }

            return found;
        }
    }
}
