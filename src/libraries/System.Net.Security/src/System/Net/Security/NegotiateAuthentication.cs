// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace System.Net.Security
{
    public sealed class NegotiateAuthentication : IDisposable
    {
        private NTAuthentication _ntAuthentication;
        private bool _isServer;
        private IIdentity? _remoteIdentity;

        public NegotiateAuthentication(NegotiateAuthenticationClientOptions clientOptions)
        {
            ContextFlagsPal contextFlags = ContextFlagsPal.Connection | clientOptions.RequiredProtectionLevel switch
            {
                ProtectionLevel.Sign => ContextFlagsPal.InitIntegrity,
                ProtectionLevel.EncryptAndSign => ContextFlagsPal.InitIntegrity | ContextFlagsPal.Confidentiality,
                _ => 0
            };

            _isServer = false;
            _ntAuthentication = new NTAuthentication(
                isServer: false,
                clientOptions.Package,
                clientOptions.Credential,
                clientOptions.TargetName,
                contextFlags,
                clientOptions.Binding);
        }

        public NegotiateAuthentication(NegotiateAuthenticationServerOptions serverOptions)
        {
            ContextFlagsPal contextFlags = ContextFlagsPal.Connection | serverOptions.RequiredProtectionLevel switch
            {
                ProtectionLevel.Sign => ContextFlagsPal.AcceptIntegrity,
                ProtectionLevel.EncryptAndSign => ContextFlagsPal.AcceptIntegrity | ContextFlagsPal.Confidentiality,
                _ => 0
            };

            _isServer = true;
            _ntAuthentication = new NTAuthentication(
                isServer: true,
                serverOptions.Package,
                serverOptions.Credential,
                null,
                contextFlags,
                serverOptions.Binding);
        }

        public void Dispose()
        {
            _ntAuthentication.CloseContext();
        }

        public bool IsAuthenticated => _ntAuthentication.IsCompleted;

        public ProtectionLevel ProtectionLevel
        {
            get => IsSigned ? (IsEncrypted ? ProtectionLevel.EncryptAndSign : ProtectionLevel.Sign) : ProtectionLevel.None;
        }

        public bool IsSigned => _ntAuthentication.IsIntegrityFlag;

        public bool IsEncrypted => _ntAuthentication.IsConfidentialityFlag;

        public bool IsMutuallyAuthenticated => _ntAuthentication.IsMutualAuthFlag;

        public bool IsServer => _isServer;

        public string Package => _ntAuthentication.ProtocolName;

        public string? TargetName => IsServer ? _ntAuthentication.ClientSpecifiedSpn : _ntAuthentication.Spn;

        public IIdentity RemoteIdentity
        {
            get
            {
                IIdentity? identity = _remoteIdentity;
                if (identity is null)
                {
                    if (IsServer)
                    {
                        // Server authentication is not supported on tvOS
                        Debug.Assert(!OperatingSystem.IsTvOS());
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

        public byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
        {
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
                SecurityStatusPalErrorCode.UnsupportedPreauth => NegotiateAuthenticationStatusCode.Unsupported,
                SecurityStatusPalErrorCode.BadBinding => NegotiateAuthenticationStatusCode.BadBinding,

                // Processing partial inputs is not supported, so this is result of incorrect input
                SecurityStatusPalErrorCode.IncompleteMessage => NegotiateAuthenticationStatusCode.InvalidToken,

                _ => NegotiateAuthenticationStatusCode.GenericFailure,
            };

            return blob;
        }

        public string? GetOutgoingBlob(string? incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
        {
            byte[]? decodedIncomingBlob = null;
            if (incomingBlob != null && incomingBlob.Length > 0)
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
