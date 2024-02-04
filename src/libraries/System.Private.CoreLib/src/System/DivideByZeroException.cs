// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an attempt to divide an integral or <see cref="decimal"/> value by zero.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DivideByZeroException : ArithmeticException
    {
        public DivideByZeroException()
            : base(SR.Arg_DivideByZero)
        {
            HResult = HResults.COR_E_DIVIDEBYZERO;
        }

        public DivideByZeroException(string? message)
            : base(message ?? SR.Arg_DivideByZero)
        {
            HResult = HResults.COR_E_DIVIDEBYZERO;
        }

        public DivideByZeroException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_DivideByZero, innerException)
        {
            HResult = HResults.COR_E_DIVIDEBYZERO;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected DivideByZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
