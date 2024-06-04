// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// Exception thrown on a stack overflow.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class StackOverflowException : SystemException
    {
        public StackOverflowException()
            : base(SR.Arg_StackOverflowException)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(string? message)
            : base(message ?? SR.Arg_StackOverflowException)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_StackOverflowException, innerException)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private StackOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
