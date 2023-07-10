// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SystemException : Exception
    {
        public SystemException()
            : base(SR.Arg_SystemException)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        public SystemException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        public SystemException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SystemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
