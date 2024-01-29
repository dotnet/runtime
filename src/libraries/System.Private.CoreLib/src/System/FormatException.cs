// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when the format of an argument is invalid, or when a composite format string is not well formed.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class FormatException : SystemException
    {
        public FormatException()
            : base(SR.Arg_FormatException)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        public FormatException(string? message)
            : base(message ?? SR.Arg_FormatException)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        public FormatException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_FormatException, innerException)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected FormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
