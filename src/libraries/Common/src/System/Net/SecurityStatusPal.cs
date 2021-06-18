// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal readonly struct SecurityStatusPal
    {
        public readonly SecurityStatusPalErrorCode ErrorCode;
        public readonly Exception? Exception;

        public SecurityStatusPal(SecurityStatusPalErrorCode errorCode, Exception? exception = null)
        {
            ErrorCode = errorCode;
            Exception = exception;
        }

        public override string ToString()
        {
            return Exception == null ?
                $"{nameof(ErrorCode)}={ErrorCode}" :
                $"{nameof(ErrorCode)}={ErrorCode}, {nameof(Exception)}={Exception}";
        }
    }

    internal enum SecurityStatusPalErrorCode
    {
        NotSet = 0,
        OK,
        ContinueNeeded,
        CompleteNeeded,
        CompAndContinue,
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
