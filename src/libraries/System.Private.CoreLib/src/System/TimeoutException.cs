// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Exception class for Timeout
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TimeoutException : SystemException
    {
        public TimeoutException()
            : base(SR.Arg_TimeoutException)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        public TimeoutException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        public TimeoutException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
