// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class TargetParameterCountException : ApplicationException
    {
        public TargetParameterCountException()
            : base(SR.Arg_TargetParameterCountException)
        {
            HResult = HResults.COR_E_TARGETPARAMCOUNT;
        }

        public TargetParameterCountException(string? message)
            : base(message ?? SR.Arg_TargetParameterCountException)
        {
            HResult = HResults.COR_E_TARGETPARAMCOUNT;
        }

        public TargetParameterCountException(string? message, Exception? inner)
            : base(message ?? SR.Arg_TargetParameterCountException, inner)
        {
            HResult = HResults.COR_E_TARGETPARAMCOUNT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private TargetParameterCountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
