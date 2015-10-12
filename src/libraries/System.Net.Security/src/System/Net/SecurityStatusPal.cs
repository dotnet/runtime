// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal enum SecurityStatusPal
    {
        NotSet = 0,
        OK,
        ContinueNeeded,
        CompleteNeeded,
        CompAndContinue,
        ContextExpired,
        CredentialsNeeded,
        Renegotiate,

        // Errors
        OutOfMemory,
        InvalidHandle,
        Unsupported,
        TargetUnknown,
        InternalError,
        PackageNotFound,
        NotOwner,
        CannotInstall,
        InvalidToken,
        CannotPack,
        QopNotSupported,
        NoImpersonation,
        LogonDenied,
        UnknownCredentials,
        NoCredentials,
        MessageAltered,
        OutOfSequence,
        NoAuthenticatingAuthority,
        IncompleteMessage,
        IncompleteCredentials,
        BufferNotEnough,
        WrongPrincipal,
        TimeSkew,
        UntrustedRoot,
        IllegalMessage,
        CertUnknown,
        CertExpired,
        AlgorithmMismatch,
        SecurityQosFailed,
        SmartcardLogonRequired,
        UnsupportedPreauth,
        BadBinding
    }
}
