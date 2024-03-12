// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an invalid attempt to access a method.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MethodAccessException : MemberAccessException
    {
        public MethodAccessException()
            : base(SR.Arg_MethodAccessException)
        {
            HResult = HResults.COR_E_METHODACCESS;
        }

        public MethodAccessException(string? message)
            : base(message ?? SR.Arg_MethodAccessException)
        {
            HResult = HResults.COR_E_METHODACCESS;
        }

        public MethodAccessException(string? message, Exception? inner)
            : base(message ?? SR.Arg_MethodAccessException, inner)
        {
            HResult = HResults.COR_E_METHODACCESS;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MethodAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
