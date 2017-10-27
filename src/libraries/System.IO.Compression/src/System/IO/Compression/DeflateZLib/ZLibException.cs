// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO.Compression
{
    /// <summary>
    /// This is the exception that is thrown when a ZLib returns an error code indicating an unrecoverable error.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ZLibException : IOException, ISerializable
    {
        private readonly string _zlibErrorContext = string.Empty;
        private readonly string _zlibErrorMessage = string.Empty;
        private readonly ZLibNative.ErrorCode _zlibErrorCode = ZLibNative.ErrorCode.Ok;

        /// <summary>
        /// This is the preferred constructor to use.
        /// The other constructors are provided for compliance to Fx design guidelines.
        /// </summary>
        /// <param name="message">A (localised) human readable error description.</param>
        /// <param name="zlibErrorContext">A description of the context within zlib where the error occurred (e.g. the function name).</param>
        /// <param name="zlibErrorCode">The error code returned by a ZLib function that caused this exception.</param>
        /// <param name="zlibErrorMessage">The string provided by ZLib as error information (unlocalised).</param>
        public ZLibException(string message, string zlibErrorContext, int zlibErrorCode, string zlibErrorMessage) : base(message)
        {
            _zlibErrorContext = zlibErrorContext;
            _zlibErrorCode = (ZLibNative.ErrorCode)zlibErrorCode;
            _zlibErrorMessage = zlibErrorMessage;
        }

        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public ZLibException(string message, string zlibErrorContext, ZLibNative.ErrorCode zlibErrorCode, string zlibErrorMessage)</code>.
        /// </summary>
        public ZLibException() { }

        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public ZLibException(string message, string zlibErrorContext, ZLibNative.ErrorCode zlibErrorCode, string zlibErrorMessage)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a <code>null</code>.</param>
        public ZLibException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new ZLibException with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ZLibException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _zlibErrorContext = info.GetString("zlibErrorContext");
            _zlibErrorCode = (ZLibNative.ErrorCode)info.GetInt32("zlibErrorCode");
            _zlibErrorMessage = info.GetString("zlibErrorMessage");
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
            si.AddValue("zlibErrorContext", _zlibErrorContext);
            si.AddValue("zlibErrorCode", (int)_zlibErrorCode);
            si.AddValue("zlibErrorMessage", _zlibErrorMessage);
        }
    }
}
