// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Net
{
    internal static class SecurityStatusAdapterPal
    {
        private const int StatusDictionarySize = 43;

#if DEBUG
        static SecurityStatusAdapterPal()
        {
            Debug.Assert(s_statusDictionary.Count == StatusDictionarySize, $"Expected size {StatusDictionarySize}, got size {s_statusDictionary.Count}");
        }
#endif

        private static readonly BidirectionalDictionary<Interop.SECURITY_STATUS, SecurityStatusPalErrorCode> s_statusDictionary = new BidirectionalDictionary<Interop.SECURITY_STATUS, SecurityStatusPalErrorCode>(StatusDictionarySize)
        {
            { Interop.SECURITY_STATUS.AlgorithmMismatch, SecurityStatusPalErrorCode.AlgorithmMismatch },
            { Interop.SECURITY_STATUS.ApplicationProtocolMismatch, SecurityStatusPalErrorCode.ApplicationProtocolMismatch },
            { Interop.SECURITY_STATUS.BadBinding, SecurityStatusPalErrorCode.BadBinding },
            { Interop.SECURITY_STATUS.BufferNotEnough, SecurityStatusPalErrorCode.BufferNotEnough },
            { Interop.SECURITY_STATUS.CannotInstall, SecurityStatusPalErrorCode.CannotInstall },
            { Interop.SECURITY_STATUS.CannotPack, SecurityStatusPalErrorCode.CannotPack },
            { Interop.SECURITY_STATUS.CertExpired, SecurityStatusPalErrorCode.CertExpired },
            { Interop.SECURITY_STATUS.CertUnknown, SecurityStatusPalErrorCode.CertUnknown },
            { Interop.SECURITY_STATUS.CompAndContinue, SecurityStatusPalErrorCode.CompAndContinue },
            { Interop.SECURITY_STATUS.CompleteNeeded, SecurityStatusPalErrorCode.CompleteNeeded },
            { Interop.SECURITY_STATUS.ContextExpired, SecurityStatusPalErrorCode.ContextExpired },
            { Interop.SECURITY_STATUS.ContinueNeeded, SecurityStatusPalErrorCode.ContinueNeeded },
            { Interop.SECURITY_STATUS.CredentialsNeeded, SecurityStatusPalErrorCode.CredentialsNeeded },
            { Interop.SECURITY_STATUS.DecryptFailure, SecurityStatusPalErrorCode.DecryptFailure },
            { Interop.SECURITY_STATUS.DowngradeDetected, SecurityStatusPalErrorCode.DowngradeDetected },
            { Interop.SECURITY_STATUS.IllegalMessage, SecurityStatusPalErrorCode.IllegalMessage },
            { Interop.SECURITY_STATUS.IncompleteCredentials, SecurityStatusPalErrorCode.IncompleteCredentials },
            { Interop.SECURITY_STATUS.IncompleteMessage, SecurityStatusPalErrorCode.IncompleteMessage },
            { Interop.SECURITY_STATUS.InternalError, SecurityStatusPalErrorCode.InternalError },
            { Interop.SECURITY_STATUS.InvalidHandle, SecurityStatusPalErrorCode.InvalidHandle },
            { Interop.SECURITY_STATUS.InvalidToken, SecurityStatusPalErrorCode.InvalidToken },
            { Interop.SECURITY_STATUS.LogonDenied, SecurityStatusPalErrorCode.LogonDenied },
            { Interop.SECURITY_STATUS.MessageAltered, SecurityStatusPalErrorCode.MessageAltered },
            { Interop.SECURITY_STATUS.NoAuthenticatingAuthority, SecurityStatusPalErrorCode.NoAuthenticatingAuthority },
            { Interop.SECURITY_STATUS.NoImpersonation, SecurityStatusPalErrorCode.NoImpersonation },
            { Interop.SECURITY_STATUS.NoCredentials, SecurityStatusPalErrorCode.NoCredentials },
            { Interop.SECURITY_STATUS.NotOwner, SecurityStatusPalErrorCode.NotOwner },
            { Interop.SECURITY_STATUS.OK, SecurityStatusPalErrorCode.OK },
            { Interop.SECURITY_STATUS.OutOfMemory, SecurityStatusPalErrorCode.OutOfMemory },
            { Interop.SECURITY_STATUS.OutOfSequence, SecurityStatusPalErrorCode.OutOfSequence },
            { Interop.SECURITY_STATUS.PackageNotFound, SecurityStatusPalErrorCode.PackageNotFound },
            { Interop.SECURITY_STATUS.QopNotSupported, SecurityStatusPalErrorCode.QopNotSupported },
            { Interop.SECURITY_STATUS.Renegotiate, SecurityStatusPalErrorCode.Renegotiate },
            { Interop.SECURITY_STATUS.SecurityQosFailed, SecurityStatusPalErrorCode.SecurityQosFailed },
            { Interop.SECURITY_STATUS.SmartcardLogonRequired, SecurityStatusPalErrorCode.SmartcardLogonRequired },
            { Interop.SECURITY_STATUS.TargetUnknown, SecurityStatusPalErrorCode.TargetUnknown },
            { Interop.SECURITY_STATUS.TimeSkew, SecurityStatusPalErrorCode.TimeSkew },
            { Interop.SECURITY_STATUS.UnknownCredentials, SecurityStatusPalErrorCode.UnknownCredentials },
            { Interop.SECURITY_STATUS.UnsupportedPreauth, SecurityStatusPalErrorCode.UnsupportedPreauth },
            { Interop.SECURITY_STATUS.Unsupported, SecurityStatusPalErrorCode.Unsupported },
            { Interop.SECURITY_STATUS.UntrustedRoot, SecurityStatusPalErrorCode.UntrustedRoot },
            { Interop.SECURITY_STATUS.WrongPrincipal, SecurityStatusPalErrorCode.WrongPrincipal },
            { Interop.SECURITY_STATUS.NoRenegotiation, SecurityStatusPalErrorCode.NoRenegotiation }
        };

        internal static SecurityStatusPal GetSecurityStatusPalFromNativeInt(int win32SecurityStatus)
        {
            return GetSecurityStatusPalFromInterop((Interop.SECURITY_STATUS)win32SecurityStatus);
        }

        internal static SecurityStatusPal GetSecurityStatusPalFromInterop(Interop.SECURITY_STATUS win32SecurityStatus, bool attachException = false)
        {
            SecurityStatusPalErrorCode statusCode;

            if (!s_statusDictionary.TryGetForward(win32SecurityStatus, out statusCode))
            {
                Debug.Fail("Unknown Interop.SecurityStatus value: " + win32SecurityStatus);
                throw new InternalException(win32SecurityStatus);
            }

            if (attachException)
            {
                return new SecurityStatusPal(statusCode, new Win32Exception((int)win32SecurityStatus));
            }
            else
            {
                return new SecurityStatusPal(statusCode);
            }
        }

        internal static Interop.SECURITY_STATUS GetInteropFromSecurityStatusPal(SecurityStatusPal status)
        {
            Interop.SECURITY_STATUS interopStatus;
            if (!s_statusDictionary.TryGetBackward(status.ErrorCode, out interopStatus))
            {
                Debug.Fail("Unknown SecurityStatus value: " + status);
                throw new InternalException(status.ErrorCode);
            }
            return interopStatus;
        }
    }
}
