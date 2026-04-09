// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an invalid attempt to access a private or protected field inside a class.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class FieldAccessException : MemberAccessException
    {
        public FieldAccessException()
            : base(SR.Arg_FieldAccessException)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message)
            : base(message ?? SR.Arg_FieldAccessException)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message, Exception? inner)
            : base(message ?? SR.Arg_FieldAccessException, inner)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected FieldAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
