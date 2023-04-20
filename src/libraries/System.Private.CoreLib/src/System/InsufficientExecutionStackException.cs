// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class InsufficientExecutionStackException : SystemException
    {
        public InsufficientExecutionStackException()
            : base(SR.Arg_InsufficientExecutionStackException)
        {
            HResult = HResults.COR_E_INSUFFICIENTEXECUTIONSTACK;
        }

        public InsufficientExecutionStackException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_INSUFFICIENTEXECUTIONSTACK;
        }

        public InsufficientExecutionStackException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_INSUFFICIENTEXECUTIONSTACK;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private InsufficientExecutionStackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
