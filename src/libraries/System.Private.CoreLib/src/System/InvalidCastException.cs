// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an attempt is made to access an element of an array or collection with an index that is outside its bounds.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidCastException : SystemException
    {
        public InvalidCastException()
            : base(SR.Arg_InvalidCastException)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(string? message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected InvalidCastException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
