// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Formats.Asn1
{
    [Serializable]
    public class AsnContentException : Exception
    {
        public AsnContentException()
            : base(SR.ContentException_DefaultMessage)
        {
        }

        public AsnContentException(string? message)
            : base(message ?? SR.ContentException_DefaultMessage)
        {
        }

        public AsnContentException(string? message, Exception? inner)
            : base(message ?? SR.ContentException_DefaultMessage, inner)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected AsnContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
