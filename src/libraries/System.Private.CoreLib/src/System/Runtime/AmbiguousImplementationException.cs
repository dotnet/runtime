// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime
{
    [Serializable]
    [TypeForwardedFrom("System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class AmbiguousImplementationException : Exception
    {
        public AmbiguousImplementationException()
            : base(SR.Arg_AmbiguousImplementationException_NoMessage)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string? message)
            : base(message ?? SR.Arg_AmbiguousImplementationException_NoMessage)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_AmbiguousImplementationException_NoMessage, innerException)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private AmbiguousImplementationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
