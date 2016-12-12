// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// </copyright>

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Authentication;
    using System.Text;
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    public class SyncFromAllServersErrorInformation
    {
        private SyncFromAllServersErrorCategory _category;
        private int _errorCode;
        private string _errorMessage = null;
        private string _sourceServer = null;
        private string _targetServer = null;

        internal SyncFromAllServersErrorInformation(SyncFromAllServersErrorCategory category, int errorCode, string errorMessage, string sourceServer, string targetServer)
        {
            _category = category;
            _errorCode = errorCode;
            _errorMessage = errorMessage;
            _sourceServer = sourceServer;
            _targetServer = targetServer;
        }

        public SyncFromAllServersErrorCategory ErrorCategory
        {
            get
            {
                return _category;
            }
        }

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
        }

        public string TargetServer
        {
            get
            {
                return _targetServer;
            }
        }

        public string SourceServer
        {
            get
            {
                return _sourceServer;
            }
        }
    }

    [Serializable]
    public class ActiveDirectoryObjectNotFoundException : Exception, ISerializable
    {
        private Type _objectType;
        private string _name = null;

        public ActiveDirectoryObjectNotFoundException(string message, Type type, string name) : base(message)
        {
            _objectType = type;
            _name = name;
        }

        public ActiveDirectoryObjectNotFoundException(string message, Exception inner) : base(message, inner) { }

        public ActiveDirectoryObjectNotFoundException(string message) : base(message) { }

        public ActiveDirectoryObjectNotFoundException() : base() { }

        protected ActiveDirectoryObjectNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public Type Type
        {
            get
            {
                return _objectType;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class ActiveDirectoryOperationException : Exception, ISerializable
    {
        private int _errorCode = 0;

        public ActiveDirectoryOperationException(string message, Exception inner, int errorCode) : base(message, inner)
        {
            _errorCode = errorCode;
        }

        public ActiveDirectoryOperationException(string message, int errorCode) : base(message)
        {
            _errorCode = errorCode;
        }

        public ActiveDirectoryOperationException(string message, Exception inner) : base(message, inner) { }

        public ActiveDirectoryOperationException(string message) : base(message) { }

        public ActiveDirectoryOperationException() : base(Res.GetString(Res.DSUnknownFailure)) { }

        protected ActiveDirectoryOperationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class ActiveDirectoryServerDownException : Exception, ISerializable
    {
        private int _errorCode = 0;
        private string _name = null;

        public ActiveDirectoryServerDownException(string message, Exception inner, int errorCode, string name) : base(message, inner)
        {
            _errorCode = errorCode;
            _name = name;
        }

        public ActiveDirectoryServerDownException(string message, int errorCode, string name) : base(message)
        {
            _errorCode = errorCode;
            _name = name;
        }

        public ActiveDirectoryServerDownException(string message, Exception inner) : base(message, inner) { }

        public ActiveDirectoryServerDownException(string message) : base(message) { }

        public ActiveDirectoryServerDownException() : base() { }

        protected ActiveDirectoryServerDownException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (!((_name == null) ||
                       (_name.Length == 0)))
                    return s + Environment.NewLine + Res.GetString(Res.Name, _name) + Environment.NewLine;
                else
                    return s;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class ActiveDirectoryObjectExistsException : Exception
    {
        public ActiveDirectoryObjectExistsException(string message, Exception inner) : base(message, inner) { }

        public ActiveDirectoryObjectExistsException(string message) : base(message) { }

        public ActiveDirectoryObjectExistsException() : base() { }

        protected ActiveDirectoryObjectExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class SyncFromAllServersOperationException : ActiveDirectoryOperationException, ISerializable
    {
        private SyncFromAllServersErrorInformation[] _errors = null;

        public SyncFromAllServersOperationException(string message, Exception inner, SyncFromAllServersErrorInformation[] errors) : base(message, inner)
        {
            _errors = errors;
        }

        public SyncFromAllServersOperationException(string message, Exception inner) : base(message, inner) { }

        public SyncFromAllServersOperationException(string message) : base(message) { }

        public SyncFromAllServersOperationException() : base(Res.GetString(Res.DSSyncAllFailure)) { }

        protected SyncFromAllServersOperationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public SyncFromAllServersErrorInformation[] ErrorInformation
        {
            get
            {
                if (_errors == null)
                    return new SyncFromAllServersErrorInformation[0];

                SyncFromAllServersErrorInformation[] tempError = new SyncFromAllServersErrorInformation[_errors.Length];
                for (int i = 0; i < _errors.Length; i++)
                    tempError[i] = new SyncFromAllServersErrorInformation(_errors[i].ErrorCategory, _errors[i].ErrorCode, _errors[i].ErrorMessage, _errors[i].SourceServer, _errors[i].TargetServer);

                return tempError;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class ForestTrustCollisionException : ActiveDirectoryOperationException, ISerializable
    {
        private ForestTrustRelationshipCollisionCollection _collisions = new ForestTrustRelationshipCollisionCollection();

        public ForestTrustCollisionException(string message, Exception inner, ForestTrustRelationshipCollisionCollection collisions) : base(message, inner)
        {
            _collisions = collisions;
        }

        public ForestTrustCollisionException(string message, Exception inner) : base(message, inner) { }

        public ForestTrustCollisionException(string message) : base(message) { }

        public ForestTrustCollisionException() : base(Res.GetString(Res.ForestTrustCollision)) { }

        protected ForestTrustCollisionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ForestTrustRelationshipCollisionCollection Collisions
        {
            get
            {
                return _collisions;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    internal class ExceptionHelper
    {
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
        private static int s_ERROR_CANCELLED = 1223;
        internal static int ERROR_DS_DRA_BAD_DN = 8439;
        internal static int ERROR_DS_NAME_UNPARSEABLE = 8350;
        internal static int ERROR_DS_UNKNOWN_ERROR = 8431;

        //
        // This method maps some common COM Hresults to
        // existing clr exceptions
        //

        internal static Exception GetExceptionFromCOMException(COMException e)
        {
            return GetExceptionFromCOMException(null, e);
        }

        internal static Exception GetExceptionFromCOMException(DirectoryContext context, COMException e)
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
            else if (errorCode == unchecked((int)0x80071392))
            {
                //
                // Object already exists
                //
                exception = new ActiveDirectoryObjectExistsException(errorMessage, e);
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
                //
                // ServerDown/Unavailable/Busy
                //

                if (context != null)
                {
                    exception = new ActiveDirectoryServerDownException(errorMessage, e, errorCode, context.GetServerName());
                }
                else
                {
                    exception = new ActiveDirectoryServerDownException(errorMessage, e, errorCode, null);
                }
            }
            else
            {
                //
                // Wrap the exception in a generic OperationException
                //
                exception = new ActiveDirectoryOperationException(errorMessage, e, errorCode);
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

                return new ActiveDirectoryServerDownException(errorMsg, errorCode, targetName);

            else

                return new ActiveDirectoryOperationException(errorMsg, errorCode);
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
                                       0, (int)temp, 0, sb, sb.Capacity + 1, 0);
            if (result != 0)
            {
                errorMsg = sb.ToString(0, result);
            }
            else
            {
                errorMsg = Res.GetString(Res.DSUnknown, Convert.ToString(temp, 16));
            }

            return errorMsg;
        }

        internal static SyncFromAllServersOperationException CreateSyncAllException(IntPtr errorInfo, bool singleError)
        {
            if (errorInfo == (IntPtr)0)
                return new SyncFromAllServersOperationException();

            if (singleError)
            {
                // single error
                DS_REPSYNCALL_ERRINFO error = new DS_REPSYNCALL_ERRINFO();
                Marshal.PtrToStructure(errorInfo, error);
                string message = GetErrorMessage(error.dwWin32Err, false);
                string source = Marshal.PtrToStringUni(error.pszSrcId);
                string target = Marshal.PtrToStringUni(error.pszSvrId);

                if (error.dwWin32Err == s_ERROR_CANCELLED)
                {
                    // this is a special case. the failure is because user specifies SyncAllOptions.CheckServerAlivenessOnly, ignore it here
                    return null;
                }
                else
                {
                    SyncFromAllServersErrorInformation managedError = new SyncFromAllServersErrorInformation(error.error, error.dwWin32Err, message, source, target);
                    return new SyncFromAllServersOperationException(Res.GetString(Res.DSSyncAllFailure), null, new SyncFromAllServersErrorInformation[] { managedError });
                }
            }
            else
            {
                // it is a NULL terminated array of DS_REPSYNCALL_ERRINFO
                IntPtr tempPtr = Marshal.ReadIntPtr(errorInfo);
                ArrayList errorList = new ArrayList();
                int i = 0;
                while (tempPtr != (IntPtr)0)
                {
                    DS_REPSYNCALL_ERRINFO error = new DS_REPSYNCALL_ERRINFO();
                    Marshal.PtrToStructure(tempPtr, error);
                    // this is a special case. the failure is because user specifies SyncAllOptions.CheckServerAlivenessOnly, ignore it here
                    if (error.dwWin32Err != s_ERROR_CANCELLED)
                    {
                        string message = GetErrorMessage(error.dwWin32Err, false);
                        string source = Marshal.PtrToStringUni(error.pszSrcId);
                        string target = Marshal.PtrToStringUni(error.pszSvrId);
                        SyncFromAllServersErrorInformation managedError = new SyncFromAllServersErrorInformation(error.error, error.dwWin32Err, message, source, target);

                        errorList.Add(managedError);
                    }

                    i++;
                    tempPtr = Marshal.ReadIntPtr(errorInfo, i * Marshal.SizeOf(typeof(IntPtr)));
                }
                // no error information, so we should not throw exception.
                if (errorList.Count == 0)
                    return null;

                SyncFromAllServersErrorInformation[] info = new SyncFromAllServersErrorInformation[errorList.Count];
                for (int j = 0; j < errorList.Count; j++)
                {
                    SyncFromAllServersErrorInformation tmp = (SyncFromAllServersErrorInformation)errorList[j];
                    info[j] = new SyncFromAllServersErrorInformation(tmp.ErrorCategory, tmp.ErrorCode, tmp.ErrorMessage, tmp.SourceServer, tmp.TargetServer);
                }

                return new SyncFromAllServersOperationException(Res.GetString(Res.DSSyncAllFailure), null, info);
            }
        }

        internal static Exception CreateForestTrustCollisionException(IntPtr collisionInfo)
        {
            ForestTrustRelationshipCollisionCollection collection = new ForestTrustRelationshipCollisionCollection();
            LSA_FOREST_TRUST_COLLISION_INFORMATION collision = new LSA_FOREST_TRUST_COLLISION_INFORMATION();
            Marshal.PtrToStructure(collisionInfo, collision);

            int count = collision.RecordCount;
            IntPtr addr = (IntPtr)0;
            for (int i = 0; i < count; i++)
            {
                addr = Marshal.ReadIntPtr(collision.Entries, i * Marshal.SizeOf(typeof(IntPtr)));
                LSA_FOREST_TRUST_COLLISION_RECORD record = new LSA_FOREST_TRUST_COLLISION_RECORD();
                Marshal.PtrToStructure(addr, record);

                ForestTrustCollisionType type = record.Type;
                string recordName = Marshal.PtrToStringUni(record.Name.Buffer, record.Name.Length / 2);
                TopLevelNameCollisionOptions TLNFlag = TopLevelNameCollisionOptions.None;
                DomainCollisionOptions domainFlag = DomainCollisionOptions.None;
                if (type == ForestTrustCollisionType.TopLevelName)
                {
                    TLNFlag = (TopLevelNameCollisionOptions)record.Flags;
                }
                else if (type == ForestTrustCollisionType.Domain)
                {
                    domainFlag = (DomainCollisionOptions)record.Flags;
                }
                ForestTrustRelationshipCollision tmp = new ForestTrustRelationshipCollision(type, TLNFlag, domainFlag, recordName);
                collection.Add(tmp);
            }

            ForestTrustCollisionException exception = new ForestTrustCollisionException(Res.GetString(Res.ForestTrustCollision), null, collection);
            return exception;
        }
    }
}
