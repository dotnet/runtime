// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The exception thrown when an invalid COM object is used. This happens
    /// when a the __ComObject type is used directly without having a backing
    /// class factory.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidComObjectException : SystemException
    {
        public InvalidComObjectException()
            : base(SR.Arg_InvalidComObjectException)
        {
            HResult = HResults.COR_E_INVALIDCOMOBJECT;
        }

        public InvalidComObjectException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_INVALIDCOMOBJECT;
        }

        public InvalidComObjectException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_INVALIDCOMOBJECT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected InvalidComObjectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
