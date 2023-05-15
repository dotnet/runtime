// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace System.Reflection.Metadata
{
    [Serializable]
    public partial class ImageFormatLimitationException : Exception
    {
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ImageFormatLimitationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
