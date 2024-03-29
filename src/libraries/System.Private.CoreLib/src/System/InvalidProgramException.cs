// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a program contains invalid IL or metadata.
    /// This exception is also thrown when internal runtime implementation limits have been exceeded by the program.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class InvalidProgramException : SystemException
    {
        public InvalidProgramException()
            : base(SR.InvalidProgram_Default)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        public InvalidProgramException(string? message)
            : base(message ?? SR.InvalidProgram_Default)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        public InvalidProgramException(string? message, Exception? inner)
            : base(message ?? SR.InvalidProgram_Default, inner)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private InvalidProgramException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
