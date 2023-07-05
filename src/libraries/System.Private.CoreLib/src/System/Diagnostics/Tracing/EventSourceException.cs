// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// Exception that is thrown when an error occurs during EventSource operation.
    /// </summary>
    [Serializable]
    public class EventSourceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the EventSourceException class.
        /// </summary>
        public EventSourceException() :
            base(SR.EventSource_ListenerWriteFailure) { }

        /// <summary>
        /// Initializes a new instance of the EventSourceException class with a specified error message.
        /// </summary>
        public EventSourceException(string? message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the EventSourceException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public EventSourceException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the EventSourceException class with serialized data.
        /// </summary>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected EventSourceException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal EventSourceException(Exception? innerException) :
            base(SR.EventSource_ListenerWriteFailure, innerException) { }
    }
}
