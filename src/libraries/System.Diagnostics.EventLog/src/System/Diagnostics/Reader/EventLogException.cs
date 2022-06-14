// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// describes an exception thrown from Event Log related classes.
    /// </summary>
    [Serializable]
    public class EventLogException : Exception
    {
        internal static void Throw(int errorCode)
        {
            switch (errorCode)
            {
                case 2: // ERROR_FILE_NOT_FOUND
                case 3: // ERROR_PATH_NOT_FOUND
                case 15007: // ERROR_EVT_CHANNEL_NOT_FOUND
                case 15027: // ERROR_EVT_MESSAGE_NOT_FOUND
                case 15028: // ERROR_EVT_MESSAGE_ID_NOT_FOUND
                case 15002: // ERROR_EVT_PUBLISHER_METADATA_NOT_FOUND
                    throw new EventLogNotFoundException(errorCode);

                case 13: // ERROR_INVALID_DATA
                case 15005: // ERROR_EVT_INVALID_EVENT_DATA
                    throw new EventLogInvalidDataException(errorCode);

                case 1818: // RPC_S_CALL_CANCELED is converted to ERROR_CANCELLED
                case 1223: // ERROR_CANCELLED
                    throw new OperationCanceledException();

                case 15037: // ERROR_EVT_PUBLISHER_DISABLED
                    throw new EventLogProviderDisabledException(errorCode);

                case 5: // ERROR_ACCESS_DENIED
                    throw new UnauthorizedAccessException();

                case 15011: // ERROR_EVT_QUERY_RESULT_STALE
                case 15012: // ERROR_EVT_QUERY_RESULT_INVALID_POSITION
                    throw new EventLogReadingException(errorCode);

                default:
                    throw new EventLogException(errorCode);
            }
        }

        public EventLogException() { }
        public EventLogException(string message) : base(message) { }
        public EventLogException(string message, Exception innerException) : base(message, innerException) { }
        protected EventLogException(int errorCode)
        {
            _errorCode = errorCode;
            HResult = Win32Marshal.MakeHRFromErrorCode(errorCode);
        }

        public override string Message
        {
            get
            {
                Win32Exception win32Exception = new Win32Exception(_errorCode);
                return win32Exception.Message;
            }
        }

        private readonly int _errorCode;

        protected EventLogException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            _errorCode = serializationInfo.GetInt32("errorCode");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", _errorCode, typeof(int));
        }
    }

    /// <summary>
    /// The object requested by the operation is not found.
    /// </summary>
    [Serializable]
    public class EventLogNotFoundException : EventLogException
    {
        public EventLogNotFoundException() { }
        public EventLogNotFoundException(string message) : base(message) { }
        public EventLogNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogNotFoundException(int errorCode) : base(errorCode) { }
        protected EventLogNotFoundException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }

    /// <summary>
    /// The state of the reader cursor has become invalid, most likely due to the fact
    /// that the log has been cleared.  User needs to obtain a new reader object if
    /// they wish to continue navigating result set.
    /// </summary>
    [Serializable]
    public class EventLogReadingException : EventLogException
    {
        public EventLogReadingException() { }
        public EventLogReadingException(string message) : base(message) { }
        public EventLogReadingException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogReadingException(int errorCode) : base(errorCode) { }
        protected EventLogReadingException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }

    /// <summary>
    /// Provider has been uninstalled while ProviderMetadata operations are being performed.
    /// Obtain a new ProviderMetadata object, when provider is reinstalled, to continue navigating
    /// provider's metadata.
    /// </summary>
    [Serializable]
    public class EventLogProviderDisabledException : EventLogException
    {
        public EventLogProviderDisabledException() { }
        public EventLogProviderDisabledException(string message) : base(message) { }
        public EventLogProviderDisabledException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogProviderDisabledException(int errorCode) : base(errorCode) { }
        protected EventLogProviderDisabledException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }

    /// <summary>
    /// Data obtained from the eventlog service, for the current operation, is invalid .
    /// </summary>
    [Serializable]
    public class EventLogInvalidDataException : EventLogException
    {
        public EventLogInvalidDataException() { }
        public EventLogInvalidDataException(string message) : base(message) { }
        public EventLogInvalidDataException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogInvalidDataException(int errorCode) : base(errorCode) { }
        protected EventLogInvalidDataException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
