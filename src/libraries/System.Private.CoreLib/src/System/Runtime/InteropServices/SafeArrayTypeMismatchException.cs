// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The exception is thrown when the runtime type of an array is different
    /// than the safe array sub type specified in the metadata.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SafeArrayTypeMismatchException : SystemException
    {
        public SafeArrayTypeMismatchException()
            : base(SR.Arg_SafeArrayTypeMismatchException)
        {
            HResult = HResults.COR_E_SAFEARRAYTYPEMISMATCH;
        }

        public SafeArrayTypeMismatchException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SAFEARRAYTYPEMISMATCH;
        }

        public SafeArrayTypeMismatchException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_SAFEARRAYTYPEMISMATCH;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SafeArrayTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
