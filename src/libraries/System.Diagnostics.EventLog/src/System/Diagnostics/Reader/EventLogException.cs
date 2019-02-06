// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// describes an exception thrown from Event Log related classes.
    /// </summary>
    public class EventLogException : Exception
    {
        internal static void Throw(int errorCode)
        {
            switch (errorCode)
            {
                case 2:
                case 3:
                case 15007:
                case 15027:
                case 15028:
                case 15002:
                    throw new EventLogNotFoundException(errorCode);

                case 13:
                case 15005:
                    throw new EventLogInvalidDataException(errorCode);

                case 1818: // RPC_S_CALL_CANCELED is converted to ERROR_CANCELLED
                case 1223:
                    throw new OperationCanceledException();

                case 15037:
                    throw new EventLogProviderDisabledException(errorCode);

                case 5:
                    throw new UnauthorizedAccessException();

                case 15011:
                case 15012:
                    throw new EventLogReadingException(errorCode);

                default:
                    throw new EventLogException(errorCode);
            }
        }

        public EventLogException() { }
        public EventLogException(string message) : base(message) { }
        public EventLogException(string message, Exception innerException) : base(message, innerException) { }
        protected EventLogException(int errorCode) { _errorCode = errorCode; }

        public override string Message
        {
            get
            {
                Win32Exception win32Exception = new Win32Exception(_errorCode);
                return win32Exception.Message;
            }
        }

        private int _errorCode;
    }

    /// <summary>
    /// The object requested by the operation is not found.
    /// </summary>
    public class EventLogNotFoundException : EventLogException
    {
        public EventLogNotFoundException() { }
        public EventLogNotFoundException(string message) : base(message) { }
        public EventLogNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogNotFoundException(int errorCode) : base(errorCode) { }
    }

    /// <summary>
    /// The state of the reader cursor has become invalid, most likely due to the fact
    /// that the log has been cleared.  User needs to obtain a new reader object if
    /// they wish to continue navigating result set.
    /// </summary>
    public class EventLogReadingException : EventLogException
    {
        public EventLogReadingException() { }
        public EventLogReadingException(string message) : base(message) { }
        public EventLogReadingException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogReadingException(int errorCode) : base(errorCode) { }
    }

    /// <summary>
    /// Provider has been uninstalled while ProviderMetadata operations are being performed.
    /// Obtain a new ProviderMetadata object, when provider is reinstalled, to continue navigating
    /// provider's metadata.
    /// </summary>
    public class EventLogProviderDisabledException : EventLogException
    {
        public EventLogProviderDisabledException() { }
        public EventLogProviderDisabledException(string message) : base(message) { }
        public EventLogProviderDisabledException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogProviderDisabledException(int errorCode) : base(errorCode) { }
    }

    /// <summary>
    /// Data obtained from the eventlog service, for the current operation, is invalid .
    /// </summary>
    public class EventLogInvalidDataException : EventLogException
    {
        public EventLogInvalidDataException() { }
        public EventLogInvalidDataException(string message) : base(message) { }
        public EventLogInvalidDataException(string message, Exception innerException) : base(message, innerException) { }
        internal EventLogInvalidDataException(int errorCode) : base(errorCode) { }
    }
}

