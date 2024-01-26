// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an invoked method is not supported,
    /// typically because it should have been implemented on a subclass.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NotSupportedException : SystemException
    {
        public NotSupportedException()
            : base(SR.Arg_NotSupportedException)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        public NotSupportedException(string? message)
            : base(message ?? SR.Arg_NotSupportedException)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        public NotSupportedException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_NotSupportedException, innerException)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected NotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
