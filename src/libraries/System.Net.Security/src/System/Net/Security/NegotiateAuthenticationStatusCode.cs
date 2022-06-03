// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    // Matches SecurityStatusPalErrorCode
    public enum NegotiateAuthenticationStatusCode
    {
        NotSet = 0,
        OK,
        ContinueNeeded,
        CompleteNeeded,
        CompleteAndContinue,
        ContextExpired,
        CredentialsNeeded,
        Renegotiate,
        TryAgain,

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
        DecryptFailure,
        AlgorithmMismatch,
        SecurityQosFailed,
        SmartcardLogonRequired,
        UnsupportedPreauth,
        BadBinding,
        DowngradeDetected,
        ApplicationProtocolMismatch,
        NoRenegotiation
    }
}
