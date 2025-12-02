// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a method call is invalid for the object's current state.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidOperationException : SystemException
    {
        public InvalidOperationException()
            : base(SR.Arg_InvalidOperationException)
        {
            HResult = HResults.COR_E_INVALIDOPERATION;
        }

        public InvalidOperationException(string? message)
            : base(message ?? SR.Arg_InvalidOperationException)
        {
            HResult = HResults.COR_E_INVALIDOPERATION;
        }

        public InvalidOperationException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_InvalidOperationException, innerException)
        {
            HResult = HResults.COR_E_INVALIDOPERATION;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected InvalidOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
