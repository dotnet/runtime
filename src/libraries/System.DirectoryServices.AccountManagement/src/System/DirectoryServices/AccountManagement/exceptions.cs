// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class PrincipalException : SystemException
    {
        internal PrincipalException() : base() { }

        internal PrincipalException(string message) : base(message) { }

        internal PrincipalException(string message, Exception innerException) :
                    base(message, innerException)
        { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected PrincipalException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MultipleMatchesException : PrincipalException
    {
        public MultipleMatchesException() : base() { }

        public MultipleMatchesException(string message) : base(message) { }

        public MultipleMatchesException(string message, Exception innerException) :
                base(message, innerException)
        { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected MultipleMatchesException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NoMatchingPrincipalException : PrincipalException
    {
        public NoMatchingPrincipalException() : base() { }

        public NoMatchingPrincipalException(string message) : base(message) { }

        public NoMatchingPrincipalException(string message, Exception innerException) :
                base(message, innerException)
        { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected NoMatchingPrincipalException(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PasswordException : PrincipalException
    {
        public PasswordException() : base() { }

        public PasswordException(string message) : base(message) { }

        public PasswordException(string message, Exception innerException) :
            base(message, innerException)
        { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected PasswordException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PrincipalExistsException : PrincipalException
    {
        public PrincipalExistsException() : base() { }

        public PrincipalExistsException(string message) : base(message) { }

        public PrincipalExistsException(string message, Exception innerException) :
            base(message, innerException)
        { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected PrincipalExistsException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PrincipalServerDownException : PrincipalException
    {
        private readonly int _errorCode;
        private readonly string _serverName;

        public PrincipalServerDownException() : base() { }

        public PrincipalServerDownException(string message) : base(message) { }

        public PrincipalServerDownException(string message, Exception innerException) :
            base(message, innerException)
        { }

        public PrincipalServerDownException(string message, int errorCode) : base(message)
        {
            _errorCode = errorCode;
        }
        public PrincipalServerDownException(string message, Exception innerException, int errorCode) : base(message, innerException)
        {
            _errorCode = errorCode;
        }
        public PrincipalServerDownException(string message, Exception innerException, int errorCode, string serverName) : base(message, innerException)
        {
            _errorCode = errorCode;
            _serverName = serverName;
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected PrincipalServerDownException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
            _errorCode = info.GetInt32("errorCode");
            _serverName = (string)info.GetValue("serverName", typeof(string));
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", _errorCode);
            info.AddValue("serverName", _serverName, typeof(string));
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PrincipalOperationException : PrincipalException
    {
        private readonly int _errorCode;

        public PrincipalOperationException() : base() { }

        public PrincipalOperationException(string message) : base(message) { }

        public PrincipalOperationException(string message, Exception innerException) :
            base(message, innerException)
        { }

        public PrincipalOperationException(string message, int errorCode) : base(message)
        {
            _errorCode = errorCode;
        }
        public PrincipalOperationException(string message, Exception innerException, int errorCode) : base(message, innerException)
        {
            _errorCode = errorCode;
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected PrincipalOperationException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
            _errorCode = info.GetInt32("errorCode");
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", _errorCode);
        }

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }
    }

    internal static class ExceptionHelper
    {
        private const int ERROR_NOT_ENOUGH_MEMORY = 8; // map to outofmemory exception
        private const int ERROR_OUTOFMEMORY = 14; // map to outofmemory exception
        private const int ERROR_DS_DRA_OUT_OF_MEM = 8446;    // map to outofmemory exception
        private const int ERROR_NO_SUCH_DOMAIN = 1355; // map to ActiveDirectoryServerDownException
        private const int ERROR_ACCESS_DENIED = 5; // map to UnauthorizedAccessException
        private const int ERROR_NO_LOGON_SERVERS = 1311; // map to ActiveDirectoryServerDownException
        private const int ERROR_DS_DRA_ACCESS_DENIED = 8453; // map to UnauthorizedAccessException
        private const int RPC_S_OUT_OF_RESOURCES = 1721; // map to outofmemory exception
        internal const int RPC_S_SERVER_UNAVAILABLE = 1722; // map to ActiveDirectoryServerDownException
        internal const int RPC_S_CALL_FAILED = 1726; // map to ActiveDirectoryServerDownException
        // internal const int ERROR_DS_DRA_BAD_DN = 8439; //fix error CS0414: Warning as Error: is assigned but its value is never used
        // internal const int ERROR_DS_NAME_UNPARSEABLE = 8350; //fix error CS0414: Warning as Error: is assigned but its value is never used
        // internal const int ERROR_DS_UNKNOWN_ERROR = 8431; //fix error CS0414: Warning as Error: is assigned but its value is never used

        // public const uint ERROR_HRESULT_ACCESS_DENIED = 0x80070005; //fix error CS0414: Warning as Error: is assigned but its value is never used
        public const uint ERROR_HRESULT_LOGON_FAILURE = 0x8007052E;
        public const uint ERROR_HRESULT_CONSTRAINT_VIOLATION = 0x8007202f;
        public const uint ERROR_LOGON_FAILURE = 0x31;
        // public const uint ERROR_LDAP_INVALID_CREDENTIALS = 49; //fix error CS0414: Warning as Error: is assigned but its value is never used
        //
        // This method maps some common COM Hresults to
        // existing clr exceptions
        //

        internal static Exception GetExceptionFromCOMException(COMException e)
        {
            Exception exception;
            int errorCode = e.ErrorCode;
            string errorMessage = e.Message;

            //
            // Check if we can throw a more specific exception
            //
            if (errorCode == unchecked((int)0x80070005))
            {
                //
                // Access Denied
                //
                exception = new UnauthorizedAccessException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x800708c5) || errorCode == unchecked((int)0x80070056) || errorCode == unchecked((int)0x8007052))
            {
                //
                // Password does not meet complexity requirements or old password does not match or policy restriction has been enforced.
                //
                exception = new PasswordException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x800708b0) || errorCode == unchecked((int)0x80071392))
            {
                //
                // Principal already exists
                //
                exception = new PrincipalExistsException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x8007052e))
            {
                //
                // Logon Failure
                //
                exception = new AuthenticationException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x8007202f))
            {
                //
                // Constraint Violation
                //
                exception = new InvalidOperationException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x80072035))
            {
                //
                // Unwilling to perform
                //
                exception = new InvalidOperationException(errorMessage, e);
            }
            else if (errorCode == unchecked((int)0x80070008))
            {
                //
                // No Memory
                //
                exception = new OutOfMemoryException();
            }
            else if ((errorCode == unchecked((int)0x8007203a)) || (errorCode == unchecked((int)0x8007200e)) || (errorCode == unchecked((int)0x8007200f)))
            {
                exception = new PrincipalServerDownException(errorMessage, e, errorCode, null);
            }
            else
            {
                //
                // Wrap the exception in a generic OperationException
                //
                exception = new PrincipalOperationException(errorMessage, e, errorCode);
            }

            return exception;
        }

        internal static Exception GetExceptionFromErrorCode(int errorCode)
        {
            return GetExceptionFromErrorCode(errorCode, null);
        }

        internal static Exception GetExceptionFromErrorCode(int errorCode, string targetName)
        {
            string errorMsg = GetErrorMessage(errorCode, false);

            if ((errorCode == ERROR_ACCESS_DENIED) || (errorCode == ERROR_DS_DRA_ACCESS_DENIED))

                return new UnauthorizedAccessException(errorMsg);

            else if ((errorCode == ERROR_NOT_ENOUGH_MEMORY) || (errorCode == ERROR_OUTOFMEMORY) || (errorCode == ERROR_DS_DRA_OUT_OF_MEM) || (errorCode == RPC_S_OUT_OF_RESOURCES))

                return new OutOfMemoryException();

            else if ((errorCode == ERROR_NO_LOGON_SERVERS) || (errorCode == ERROR_NO_SUCH_DOMAIN) || (errorCode == RPC_S_SERVER_UNAVAILABLE) || (errorCode == RPC_S_CALL_FAILED))
            {
                return new PrincipalServerDownException(errorMsg, errorCode);
            }
            else
            {
                return new PrincipalOperationException(errorMsg, errorCode);
            }
        }

        internal static string GetErrorMessage(int errorCode, bool hresult)
        {
            uint temp = (uint)errorCode;
            if (!hresult)
            {
                temp = ((((temp) & 0x0000FFFF) | (7 << 16) | 0x80000000));
            }

            return new Win32Exception((int)temp).Message;
        }
    }
}
