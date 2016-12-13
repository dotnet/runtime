// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

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
        private static Hashtable s_resultCodeHash = null;

        static LdapErrorMappings()
        {
            s_resultCodeHash = new Hashtable();

            s_resultCodeHash.Add(LdapError.IsLeaf, Res.GetString(Res.LDAP_IS_LEAF));
            s_resultCodeHash.Add(LdapError.InvalidCredentials, Res.GetString(Res.LDAP_INVALID_CREDENTIALS));
            s_resultCodeHash.Add(LdapError.ServerDown, Res.GetString(Res.LDAP_SERVER_DOWN));
            s_resultCodeHash.Add(LdapError.LocalError, Res.GetString(Res.LDAP_LOCAL_ERROR));
            s_resultCodeHash.Add(LdapError.EncodingError, Res.GetString(Res.LDAP_ENCODING_ERROR));
            s_resultCodeHash.Add(LdapError.DecodingError, Res.GetString(Res.LDAP_DECODING_ERROR));
            s_resultCodeHash.Add(LdapError.TimeOut, Res.GetString(Res.LDAP_TIMEOUT));
            s_resultCodeHash.Add(LdapError.AuthUnknown, Res.GetString(Res.LDAP_AUTH_UNKNOWN));
            s_resultCodeHash.Add(LdapError.FilterError, Res.GetString(Res.LDAP_FILTER_ERROR));
            s_resultCodeHash.Add(LdapError.UserCancelled, Res.GetString(Res.LDAP_USER_CANCELLED));
            s_resultCodeHash.Add(LdapError.ParameterError, Res.GetString(Res.LDAP_PARAM_ERROR));
            s_resultCodeHash.Add(LdapError.NoMemory, Res.GetString(Res.LDAP_NO_MEMORY));
            s_resultCodeHash.Add(LdapError.ConnectError, Res.GetString(Res.LDAP_CONNECT_ERROR));
            s_resultCodeHash.Add(LdapError.NotSupported, Res.GetString(Res.LDAP_NOT_SUPPORTED));
            s_resultCodeHash.Add(LdapError.NoResultsReturned, Res.GetString(Res.LDAP_NO_RESULTS_RETURNED));
            s_resultCodeHash.Add(LdapError.ControlNotFound, Res.GetString(Res.LDAP_CONTROL_NOT_FOUND));
            s_resultCodeHash.Add(LdapError.MoreResults, Res.GetString(Res.LDAP_MORE_RESULTS_TO_RETURN));
            s_resultCodeHash.Add(LdapError.ClientLoop, Res.GetString(Res.LDAP_CLIENT_LOOP));
            s_resultCodeHash.Add(LdapError.ReferralLimitExceeded, Res.GetString(Res.LDAP_REFERRAL_LIMIT_EXCEEDED));
            s_resultCodeHash.Add(LdapError.SendTimeOut, Res.GetString(Res.LDAP_SEND_TIMEOUT));
        }

        /// <summary>
        /// This function maps a string containing a DSML v2 errorResult into a LDAPResultCode.
        /// </summary>
		static public string MapResultCode(int errorCode)
        {
            return (string)s_resultCodeHash[(LdapError)errorCode];
        }
    }

    [Serializable]
    public class LdapException : DirectoryException, ISerializable
    {
        private int _errorCode;
        private string _serverErrorMessage;
        internal PartialResultsCollection results = new PartialResultsCollection();
        protected LdapException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public LdapException() : base() { }

        public LdapException(string message) : base(message) { }

        public LdapException(string message, Exception inner) : base(message, inner) { }

        public LdapException(int errorCode) : base(Res.GetString(Res.DefaultLdapError))
        {
            _errorCode = errorCode;
        }

        public LdapException(int errorCode, string message) : base(message)
        {
            _errorCode = errorCode;
        }

        public LdapException(int errorCode, string message, string serverErrorMessage) : base(message)
        {
            _errorCode = errorCode;
            _serverErrorMessage = serverErrorMessage;
        }

        public LdapException(int errorCode, string message, Exception inner) : base(message, inner)
        {
            _errorCode = errorCode;
        }

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        public string ServerErrorMessage
        {
            get
            {
                return _serverErrorMessage;
            }
        }

        public PartialResultsCollection PartialResults
        {
            get
            {
                return this.results;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class TlsOperationException : DirectoryOperationException
    {
        protected TlsOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

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
            string errorMessage;
            if (error != (int)ResultCode.Success)
            {
                if (Utility.IsResultCode((ResultCode)error))
                {
                    errorMessage = OperationErrorMappings.MapResultCode(error);
                    throw new DirectoryOperationException(null, errorMessage);
                }
                else if (Utility.IsLdapError((LdapError)error))
                {
                    errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }
                else
                    throw new LdapException(error);
            }
        }
    }
}
