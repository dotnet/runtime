// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class KeyNotFoundException : SystemException
    {
        public KeyNotFoundException()
            : base(SR.Arg_KeyNotFound)
        {
            HResult = HResults.COR_E_KEYNOTFOUND;
        }

        public KeyNotFoundException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_KEYNOTFOUND;
        }

        public KeyNotFoundException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_KEYNOTFOUND;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected KeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
