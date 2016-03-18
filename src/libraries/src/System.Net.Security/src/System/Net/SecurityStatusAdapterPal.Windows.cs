// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net
{
    internal static class SecurityStatusAdapterPal
    {
        private static readonly BidirectionalDictionary<Interop.SecurityStatus, SecurityStatusPalErrorCode> s_statusDictionary = new BidirectionalDictionary<Interop.SecurityStatus, SecurityStatusPalErrorCode>(39)
        {
            { Interop.SecurityStatus.AlgorithmMismatch, SecurityStatusPalErrorCode.AlgorithmMismatch },
            { Interop.SecurityStatus.BadBinding, SecurityStatusPalErrorCode.BadBinding },
            { Interop.SecurityStatus.BufferNotEnough, SecurityStatusPalErrorCode.BufferNotEnough },
            { Interop.SecurityStatus.CannotInstall, SecurityStatusPalErrorCode.CannotInstall },
            { Interop.SecurityStatus.CertExpired, SecurityStatusPalErrorCode.CertExpired },
            { Interop.SecurityStatus.CertUnknown, SecurityStatusPalErrorCode.CertUnknown },
            { Interop.SecurityStatus.CompAndContinue, SecurityStatusPalErrorCode.CompAndContinue },
            { Interop.SecurityStatus.CompleteNeeded, SecurityStatusPalErrorCode.CompleteNeeded },
            { Interop.SecurityStatus.ContextExpired, SecurityStatusPalErrorCode.ContextExpired },
            { Interop.SecurityStatus.CredentialsNeeded, SecurityStatusPalErrorCode.CredentialsNeeded },
            { Interop.SecurityStatus.ContinueNeeded, SecurityStatusPalErrorCode.ContinueNeeded },
            { Interop.SecurityStatus.IllegalMessage, SecurityStatusPalErrorCode.IllegalMessage },
            { Interop.SecurityStatus.CannotPack, SecurityStatusPalErrorCode.CannotPack },
            { Interop.SecurityStatus.IncompleteCredentials, SecurityStatusPalErrorCode.IncompleteCredentials },
            { Interop.SecurityStatus.IncompleteMessage, SecurityStatusPalErrorCode.IncompleteMessage },
            { Interop.SecurityStatus.InternalError, SecurityStatusPalErrorCode.InternalError },
            { Interop.SecurityStatus.InvalidHandle, SecurityStatusPalErrorCode.InvalidHandle },
            { Interop.SecurityStatus.InvalidToken, SecurityStatusPalErrorCode.InvalidToken },
            { Interop.SecurityStatus.LogonDenied, SecurityStatusPalErrorCode.LogonDenied },
            { Interop.SecurityStatus.NoAuthenticatingAuthority, SecurityStatusPalErrorCode.NoAuthenticatingAuthority },
            { Interop.SecurityStatus.NoImpersonation, SecurityStatusPalErrorCode.NoImpersonation },
            { Interop.SecurityStatus.NoCredentials, SecurityStatusPalErrorCode.NoCredentials },
            { Interop.SecurityStatus.NotOwner, SecurityStatusPalErrorCode.NotOwner },
            { Interop.SecurityStatus.OK, SecurityStatusPalErrorCode.OK },
            { Interop.SecurityStatus.OutOfMemory, SecurityStatusPalErrorCode.OutOfMemory },
            { Interop.SecurityStatus.OutOfSequence, SecurityStatusPalErrorCode.OutOfSequence },
            { Interop.SecurityStatus.PackageNotFound, SecurityStatusPalErrorCode.PackageNotFound },
            { Interop.SecurityStatus.MessageAltered, SecurityStatusPalErrorCode.MessageAltered },
            { Interop.SecurityStatus.QopNotSupported, SecurityStatusPalErrorCode.QopNotSupported },
            { Interop.SecurityStatus.Renegotiate, SecurityStatusPalErrorCode.Renegotiate },
            { Interop.SecurityStatus.SecurityQosFailed, SecurityStatusPalErrorCode.SecurityQosFailed },
            { Interop.SecurityStatus.SmartcardLogonRequired, SecurityStatusPalErrorCode.SmartcardLogonRequired },
            { Interop.SecurityStatus.TargetUnknown, SecurityStatusPalErrorCode.TargetUnknown },
            { Interop.SecurityStatus.TimeSkew, SecurityStatusPalErrorCode.TimeSkew },
            { Interop.SecurityStatus.UnknownCredentials, SecurityStatusPalErrorCode.UnknownCredentials },
            { Interop.SecurityStatus.UnsupportedPreauth, SecurityStatusPalErrorCode.UnsupportedPreauth },
            { Interop.SecurityStatus.Unsupported, SecurityStatusPalErrorCode.Unsupported },
            { Interop.SecurityStatus.UntrustedRoot, SecurityStatusPalErrorCode.UntrustedRoot },
            { Interop.SecurityStatus.WrongPrincipal, SecurityStatusPalErrorCode.WrongPrincipal }
        };

        internal static SecurityStatusPal GetSecurityStatusPalFromNativeInt(int win32SecurityStatus)
        {
            return GetSecurityStatusPalFromInterop((Interop.SecurityStatus) win32SecurityStatus);
        }

        internal static SecurityStatusPal GetSecurityStatusPalFromInterop(Interop.SecurityStatus win32SecurityStatus)
        {
            SecurityStatusPalErrorCode statusCode;

            if (!s_statusDictionary.TryGetForward(win32SecurityStatus, out statusCode))
            {
                Debug.Fail("Unknown Interop.SecurityStatus value: " + win32SecurityStatus);
                throw new InternalException();
            }
            return new SecurityStatusPal(statusCode);
        }

        internal static Interop.SecurityStatus GetInteropFromSecurityStatusPal(SecurityStatusPal status)
        {
            Interop.SecurityStatus interopStatus;
            if (!s_statusDictionary.TryGetBackward(status.ErrorCode, out interopStatus))
            {
                Debug.Fail("Unknown SecurityStatus value: " + status);
                throw new InternalException();
            }
            return interopStatus;
        }
    }
}
