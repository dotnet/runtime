// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: An exception class to indicate that the Thread class is in an
**          invalid state for the method.
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ThreadStateException : SystemException
    {
        public ThreadStateException()
            : base(SR.Arg_ThreadStateException)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        public ThreadStateException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        public ThreadStateException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ThreadStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
