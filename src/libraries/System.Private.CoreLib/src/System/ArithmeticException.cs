// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown for errors in an arithmetic, casting, or conversion operation.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ArithmeticException : SystemException
    {
        // Creates a new ArithmeticException with its message string set to
        // the empty string, its HRESULT set to COR_E_ARITHMETIC,
        // and its ExceptionInfo reference set to null.
        public ArithmeticException()
            : base(SR.Arg_ArithmeticException)
        {
            HResult = HResults.COR_E_ARITHMETIC;
        }

        // Creates a new ArithmeticException with its message string set to
        // message, its HRESULT set to COR_E_ARITHMETIC,
        // and its ExceptionInfo reference set to null.
        //
        public ArithmeticException(string? message)
            : base(message ?? SR.Arg_ArithmeticException)
        {
            HResult = HResults.COR_E_ARITHMETIC;
        }

        public ArithmeticException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_ArithmeticException, innerException)
        {
            HResult = HResults.COR_E_ARITHMETIC;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ArithmeticException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
