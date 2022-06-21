// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiateAuthentication"/>
        /// for client-side authentication session.
        /// </summary>
        /// <param name="clientOptions">The property bag for the authentication options.</param>
        public NegotiateAuthentication(NegotiateAuthenticationClientOptions clientOptions)
        {
            ArgumentNullException.ThrowIfNull(clientOptions);

            ContextFlagsPal contextFlags = clientOptions.RequiredProtectionLevel switch
            {
                ProtectionLevel.Sign => ContextFlagsPal.InitIntegrity,
                ProtectionLevel.EncryptAndSign => ContextFlagsPal.InitIntegrity | ContextFlagsPal.Confidentiality,
                _ => 0
            } | ContextFlagsPal.Connection;

            _isServer = false;
            _requestedPackage = clientOptions.Package;
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

            _isServer = true;
            _requestedPackage = serverOptions.Package;
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
    }
}
