// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Formats.Cbor
{
    /// <summary>The exception that's thrown when CBOR data is invalid.</summary>
    [Serializable]
    public class CborContentException : Exception
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="CborContentException" /> class, using the provided message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public CborContentException(string? message)
            : base(message ?? SR.CborContentException_DefaultMessage)
        {

        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CborContentException" /> class,
        ///  using the provided message and exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public CborContentException(string? message, Exception? inner)
            : base(message ?? SR.CborContentException_DefaultMessage, inner)
        {

        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CborContentException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected CborContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
