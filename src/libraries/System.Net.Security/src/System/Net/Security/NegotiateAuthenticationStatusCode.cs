// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    public enum NegotiateAuthenticationStatusCode
    {
        Completed = 0, // GSS_S_COMPLETE
        ContinueNeeded, // GSS_S_CONTINUE_NEEDED

        GenericFailure, // GSS_S_FAILURE/GSS_S_NO_CONTEXT
        BadBinding, // GSS_S_BAD_BINDINGS
        Unsupported, // GSS_S_BAD_MECH (Unsupported mechanism)
        MessageAltered, // GSS_S_BAD_SIG = GSS_S_BAD_MIC
        ContextExpired, // GSS_S_CONTEXT_EXPIRED
        CredentialsExpired, // GSS_S_CREDENTIALS_EXPIRED
        InvalidCredentials, // GSS_S_DEFECTIVE_CREDENTIAL
        InvalidToken, // GSS_S_DEFECTIVE_TOKEN
        UnknownCredentials, // GSS_S_NO_CRED
        QopNotSupported, // GSS_S_BAD_QOP
        OutOfSequence, // GSS_S_DUPLICATE_TOKEN/GSS_S_OLD_TOKEN/GSS_S_UNSEQ_TOKEN/GSS_S_GAP_TOKEN + GSS_E_FAILURE
    }
}
