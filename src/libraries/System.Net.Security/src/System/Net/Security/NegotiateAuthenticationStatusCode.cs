// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    /// <summary>
    /// Represents a status code for single step of an authentication exchange.
    /// </summary>
    public enum NegotiateAuthenticationStatusCode
    {
        /// <summary>Operation completed successfully.</summary>
        /// <remarks>Maps to GSS_S_COMPLETE status in GSSAPI.</remarks>
        Completed = 0,

        /// <summary>Operation completed successfully but more tokens are to be exchanged with the other party.</summary>
        /// <remarks>Maps to GSS_S_CONTINUE_NEEDED status in GSSAPI.</remarks>
        ContinueNeeded,

        /// <summary>Operation resulted in failure but not specific error code was given.</summary>
        /// <remarks>Maps to GSS_S_FAILURE status in GSSAPI.</remarks>
        GenericFailure,

        /// <summary>Channel binding mismatch between client and server.</summary>
        /// <remarks>Maps to GSS_S_BAD_BINDINGS status in GSSAPI.</remarks>
        BadBinding,

        /// <summary>Unsupported authentication package was requested.</summary>
        /// <remarks>Maps to GSS_S_BAD_MECH status in GSSAPI.</remarks>
        Unsupported,

        /// <summary>Message was altered and failed an integrity check validation.</summary>
        /// <remarks>Maps to GSS_S_BAD_SIG or GSS_S_BAD_MIC status in GSSAPI.</remarks>
        MessageAltered,

        /// <summary>Referenced authentication context has expired.</summary>
        /// <remarks>Maps to GSS_S_CONTEXT_EXPIRED status in GSSAPI.</remarks>
        ContextExpired,

        /// <summary>Authentication credentials have expired.</summary>
        /// <remarks>Maps to GSS_S_CREDENTIALS_EXPIRED status in GSSAPI.</remarks>
        CredentialsExpired,

        /// <summary>Consistency checks performed on the credential failed.</summary>
        /// <remarks>Maps to GSS_S_DEFECTIVE_CREDENTIAL status in GSSAPI.</remarks>
        InvalidCredentials,

        /// <summary>Checks performed on the authentication token failed.</summary>
        /// <remarks>Maps to GSS_S_DEFECTIVE_TOKEN status in GSSAPI.</remarks>
        InvalidToken,

        /// <summary>The supplied credentials were not valid for context acceptance, or the credential handle did not reference any credentials.</summary>
        /// <remarks>Maps to GSS_S_NO_CRED status in GSSAPI.</remarks>
        UnknownCredentials,

        /// <summary>Requested protection level is not supported.</summary>
        /// <remarks>Maps to GSS_S_BAD_QOP status in GSSAPI.</remarks>
        QopNotSupported,

        /// <summary>Authentication token was identfied as duplicate, old, or out of expected sequence.</summary>
        /// <remarks>Maps to GSS_S_DUPLICATE_TOKEN, GSS_S_OLD_TOKEN, GSS_S_UNSEQ_TOKEN, and GSS_S_GAP_TOKEN status bits in GSSAPI when failure was indicated.</remarks>
        OutOfSequence,

        /// <status>Validation of RequiredProtectionLevel against negotiated protection level failed.</status>
        SecurityQosFailed,

        /// <status>Validation of the target name failed</status>
        TargetUnknown,

        /// <status>Validation of the impersonation level failed</status>
        ImpersonationValidationFailed,
    }
}
