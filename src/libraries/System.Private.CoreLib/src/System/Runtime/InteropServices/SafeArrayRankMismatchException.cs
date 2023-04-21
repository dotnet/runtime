// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The exception is thrown when the runtime rank of a safe array is different
    /// than the array rank specified in the metadata.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SafeArrayRankMismatchException : SystemException
    {
        public SafeArrayRankMismatchException()
            : base(SR.Arg_SafeArrayRankMismatchException)
        {
            HResult = HResults.COR_E_SAFEARRAYRANKMISMATCH;
        }

        public SafeArrayRankMismatchException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SAFEARRAYRANKMISMATCH;
        }

        public SafeArrayRankMismatchException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_SAFEARRAYRANKMISMATCH;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SafeArrayRankMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
