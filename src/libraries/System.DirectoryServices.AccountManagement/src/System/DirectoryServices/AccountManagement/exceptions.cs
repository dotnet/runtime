// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Authentication;
using System.Security.Permissions;

namespace System.DirectoryServices.AccountManagement
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    abstract public class PrincipalException : SystemException
    {
        internal PrincipalException() : base() { }

        internal PrincipalException(string message) : base(message) { }

        internal PrincipalException(string message, Exception innerException) :
                    base(message, innerException)
        { }

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

        protected NoMatchingPrincipalException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
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

        protected PrincipalExistsException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PrincipalServerDownException : PrincipalException
    {
        private int _errorCode = 0;
        private string _serverName = null;

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

        protected PrincipalServerDownException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
            _errorCode = info.GetInt32("errorCode");
            _serverName = (string)info.GetValue("serverName", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", _errorCode);
            info.AddValue("serverName", _serverName, typeof(String));
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PrincipalOperationException : PrincipalException
    {
        private int _errorCode = 0;

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

        protected PrincipalOperationException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
        {
            _errorCode = info.GetInt32("errorCode");
        }

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

    internal class ExceptionHelper
    {
        // Put a private constructor because this class should only be used as static methods
        private ExceptionHelper() { }

        private static int s_ERROR_NOT_ENOUGH_MEMORY = 8; // map to outofmemory exception
        private static int s_ERROR_OUTOFMEMORY = 14; // map to outofmemory exception
        private static int s_ERROR_DS_DRA_OUT_OF_MEM = 8446;    // map to outofmemory exception
        private static int s_ERROR_NO_SUCH_DOMAIN = 1355; // map to ActiveDirectoryServerDownException
        private static int s_ERROR_ACCESS_DENIED = 5; // map to UnauthorizedAccessException
        private static int s_ERROR_NO_LOGON_SERVERS = 1311; // map to ActiveDirectoryServerDownException
        private static int s_ERROR_DS_DRA_ACCESS_DENIED = 8453; // map to UnauthorizedAccessException
        private static int s_RPC_S_OUT_OF_RESOURCES = 1721; // map to outofmemory exception
        internal static int RPC_S_SERVER_UNAVAILABLE = 1722; // map to ActiveDirectoryServerDownException
        internal static int RPC_S_CALL_FAILED = 1726; // map to ActiveDirectoryServerDownException
        // internal static int ERROR_DS_DRA_BAD_DN = 8439; //fix error CS0414: Warning as Error: is assigned but its value is never used
        // internal static int ERROR_DS_NAME_UNPARSEABLE = 8350; //fix error CS0414: Warning as Error: is assigned but its value is never used
        // internal static int ERROR_DS_UNKNOWN_ERROR = 8431; //fix error CS0414: Warning as Error: is assigned but its value is never used

        // public static uint ERROR_HRESULT_ACCESS_DENIED = 0x80070005; //fix error CS0414: Warning as Error: is assigned but its value is never used
        public static uint ERROR_HRESULT_LOGON_FAILURE = 0x8007052E;
        public static uint ERROR_HRESULT_CONSTRAINT_VIOLATION = 0x8007202f;
        public static uint ERROR_LOGON_FAILURE = 0x31;
        // public static uint ERROR_LDAP_INVALID_CREDENTIALS = 49; //fix error CS0414: Warning as Error: is assigned but its value is never used
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

            if ((errorCode == s_ERROR_ACCESS_DENIED) || (errorCode == s_ERROR_DS_DRA_ACCESS_DENIED))

                return new UnauthorizedAccessException(errorMsg);

            else if ((errorCode == s_ERROR_NOT_ENOUGH_MEMORY) || (errorCode == s_ERROR_OUTOFMEMORY) || (errorCode == s_ERROR_DS_DRA_OUT_OF_MEM) || (errorCode == s_RPC_S_OUT_OF_RESOURCES))

                return new OutOfMemoryException();

            else if ((errorCode == s_ERROR_NO_LOGON_SERVERS) || (errorCode == s_ERROR_NO_SUCH_DOMAIN) || (errorCode == RPC_S_SERVER_UNAVAILABLE) || (errorCode == RPC_S_CALL_FAILED))
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
            string errorMsg = "";
            StringBuilder sb = new StringBuilder(256);
            int result = UnsafeNativeMethods.FormatMessageW(UnsafeNativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS |
                                       UnsafeNativeMethods.FORMAT_MESSAGE_FROM_SYSTEM |
                                       UnsafeNativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                       IntPtr.Zero, (int)temp, 0, sb, sb.Capacity + 1, IntPtr.Zero);
            if (result != 0)
            {
                errorMsg = sb.ToString(0, result);
            }
            else
            {
                errorMsg = SR.DSUnknown + Convert.ToString(temp, 16);
            }

            return errorMsg;
        }
    }
}
