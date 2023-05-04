// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: The arrays are of different primitive types.
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    // The ArrayMismatchException is thrown when an attempt to store
    // an object of the wrong type within an array occurs.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ArrayTypeMismatchException : SystemException
    {
        // Creates a new ArrayMismatchException with its message string set to
        // the empty string, its HRESULT set to COR_E_ARRAYTYPEMISMATCH,
        // and its ExceptionInfo reference set to null.
        public ArrayTypeMismatchException()
            : base(SR.Arg_ArrayTypeMismatchException)
        {
            HResult = HResults.COR_E_ARRAYTYPEMISMATCH;
        }

        // Creates a new ArrayMismatchException with its message string set to
        // message, its HRESULT set to COR_E_ARRAYTYPEMISMATCH,
        // and its ExceptionInfo reference set to null.
        //
        public ArrayTypeMismatchException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_ARRAYTYPEMISMATCH;
        }

        public ArrayTypeMismatchException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ARRAYTYPEMISMATCH;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ArrayTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
