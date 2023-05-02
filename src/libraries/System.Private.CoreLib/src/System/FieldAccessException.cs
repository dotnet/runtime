// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class FieldAccessException : MemberAccessException
    {
        public FieldAccessException()
            : base(SR.Arg_FieldAccessException)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected FieldAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
