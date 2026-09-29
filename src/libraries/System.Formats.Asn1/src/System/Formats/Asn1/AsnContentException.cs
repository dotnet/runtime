// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Formats.Asn1
{
    /// <summary>
    ///   The exception that is thrown when an encoded ASN.1 value cannot be successfully decoded.
    /// </summary>
    [Serializable]
    public class AsnContentException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnContentException" /> class, using the default message.
        /// </summary>
        public AsnContentException()
            : base(SR.ContentException_DefaultMessage)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnContentException" /> class, using the provided message.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for the exception.
        /// </param>
        public AsnContentException(string? message)
            : base(message ?? SR.ContentException_DefaultMessage)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnContentException" /> class, using the provided message and
        ///   exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for the exception.
        /// </param>
        /// <param name="inner">
        ///   The exception that is the cause of the current exception.
        /// </param>
        public AsnContentException(string? message, Exception? inner)
            : base(message ?? SR.ContentException_DefaultMessage, inner)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnContentException" /> class with serialized data.
        /// </summary>
        /// <param name="info">
        ///   The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        ///   The contextual information about the source or destination.
        /// </param>
#if NET
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected AsnContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
