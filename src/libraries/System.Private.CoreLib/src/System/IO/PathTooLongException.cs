// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PathTooLongException : IOException
    {
        public PathTooLongException()
            : base(SR.IO_PathTooLong)
        {
            HResult = HResults.COR_E_PATHTOOLONG;
        }

        public PathTooLongException(string? message)
            : base(message ?? SR.IO_PathTooLong)
        {
            HResult = HResults.COR_E_PATHTOOLONG;
        }

        public PathTooLongException(string? message, Exception? innerException)
            : base(message ?? SR.IO_PathTooLong, innerException)
        {
            HResult = HResults.COR_E_PATHTOOLONG;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected PathTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
