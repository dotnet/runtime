// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ThreadStartException : SystemException
    {
        internal ThreadStartException()
            : base(SR.Arg_ThreadStartException)
        {
            HResult = HResults.COR_E_THREADSTART;
        }

        internal ThreadStartException(Exception reason)
            : base(SR.Arg_ThreadStartException, reason)
        {
            HResult = HResults.COR_E_THREADSTART;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private ThreadStartException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
