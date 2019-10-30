// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.DirectoryServices.Protocols
{
    internal enum LdapError
    {
        IsLeaf = 0x23,
        InvalidCredentials = 49,
        ServerDown = 0x51,
        LocalError = 0x52,
        EncodingError = 0x53,
        DecodingError = 0x54,
        TimeOut = 0x55,
        AuthUnknown = 0x56,
        FilterError = 0x57,
        UserCancelled = 0x58,
        ParameterError = 0x59,
        NoMemory = 0x5a,
        ConnectError = 0x5b,
        NotSupported = 0x5c,
        NoResultsReturned = 0x5e,
        ControlNotFound = 0x5d,
        MoreResults = 0x5f,
        ClientLoop = 0x60,
        ReferralLimitExceeded = 0x61,
        SendTimeOut = 0x70
    }

    internal class LdapErrorMappings
    {
        private static readonly Dictionary<LdapError, string> s_resultCodeMapping = new Dictionary<LdapError, string>(capacity: 20)
        {
            { LdapError.IsLeaf, SR.LDAP_IS_LEAF },
            { LdapError.InvalidCredentials, SR.LDAP_INVALID_CREDENTIALS },
            { LdapError.ServerDown, SR.LDAP_SERVER_DOWN },
            { LdapError.LocalError, SR.LDAP_LOCAL_ERROR },
            { LdapError.EncodingError, SR.LDAP_ENCODING_ERROR },
            { LdapError.DecodingError, SR.LDAP_DECODING_ERROR },
            { LdapError.TimeOut, SR.LDAP_TIMEOUT },
            { LdapError.AuthUnknown, SR.LDAP_AUTH_UNKNOWN },
            { LdapError.FilterError, SR.LDAP_FILTER_ERROR },
            { LdapError.UserCancelled, SR.LDAP_USER_CANCELLED },
            { LdapError.ParameterError, SR.LDAP_PARAM_ERROR },
            { LdapError.NoMemory, SR.LDAP_NO_MEMORY },
            { LdapError.ConnectError, SR.LDAP_CONNECT_ERROR },
            { LdapError.NotSupported, SR.LDAP_NOT_SUPPORTED },
            { LdapError.NoResultsReturned, SR.LDAP_NO_RESULTS_RETURNED },
            { LdapError.ControlNotFound, SR.LDAP_CONTROL_NOT_FOUND },
            { LdapError.MoreResults, SR.LDAP_MORE_RESULTS_TO_RETURN },
            { LdapError.ClientLoop, SR.LDAP_CLIENT_LOOP },
            { LdapError.ReferralLimitExceeded, SR.LDAP_REFERRAL_LIMIT_EXCEEDED },
            { LdapError.SendTimeOut, SR.LDAP_SEND_TIMEOUT }
        };

        public static string MapResultCode(int errorCode)
        {
            s_resultCodeMapping.TryGetValue((LdapError)errorCode, out string errorMessage);
            return errorMessage;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class LdapException : DirectoryException, ISerializable
    {
        protected LdapException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public LdapException() : base() { }

        public LdapException(string message) : base(message) { }

        public LdapException(string message, Exception inner) : base(message, inner) { }

        public LdapException(int errorCode) : base(SR.DefaultLdapError)
        {
            ErrorCode = errorCode;
        }

        public LdapException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public LdapException(int errorCode, string message, string serverErrorMessage) : base(message)
        {
            ErrorCode = errorCode;
            ServerErrorMessage = serverErrorMessage;
        }

        public LdapException(int errorCode, string message, Exception inner) : base(message, inner)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }

        public string ServerErrorMessage { get; }

        public PartialResultsCollection PartialResults { get; } = new PartialResultsCollection();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TlsOperationException : DirectoryOperationException
    {
        protected TlsOperationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public TlsOperationException() : base() { }

        public TlsOperationException(string message) : base(message) { }

        public TlsOperationException(string message, Exception inner) : base(message, inner) { }

        public TlsOperationException(DirectoryResponse response) : base(response)
        {
        }

        public TlsOperationException(DirectoryResponse response, string message) : base(response, message)
        {
        }

        public TlsOperationException(DirectoryResponse response, string message, Exception inner) : base(response, message, inner)
        {
        }
    }

    internal class ErrorChecking
    {
        public static void CheckAndSetLdapError(int error)
        {
            if (error != (int)ResultCode.Success)
            {
                if (Utility.IsResultCode((ResultCode)error))
                {
                    string errorMessage = OperationErrorMappings.MapResultCode(error);
                    throw new DirectoryOperationException(null, errorMessage);
                }
                else if (Utility.IsLdapError((LdapError)error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }
                else
                {
                    throw new LdapException(error);
                }
            }
        }
    }
}
