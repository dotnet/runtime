// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: For methods that are passed arrays with the wrong number of
**          dimensions.
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class RankException : SystemException
    {
        public RankException()
            : base(SR.Arg_RankException)
        {
            HResult = HResults.COR_E_RANK;
        }

        public RankException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_RANK;
        }

        public RankException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_RANK;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected RankException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
