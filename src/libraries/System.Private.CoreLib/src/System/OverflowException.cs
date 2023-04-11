// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Exception class for Arithmetic Overflows.
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OverflowException : ArithmeticException
    {
        public OverflowException()
            : base(SR.Arg_OverflowException)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        public OverflowException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        public OverflowException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected OverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
