// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an arithmetic, casting, or conversion operation in a checked context results in an overflow.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OverflowException : ArithmeticException
    {
        public OverflowException()
            : base(SR.Arg_OverflowException)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        public OverflowException(string? message)
            : base(message ?? SR.Arg_OverflowException)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        public OverflowException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_OverflowException, innerException)
        {
            HResult = HResults.COR_E_OVERFLOW;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected OverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
