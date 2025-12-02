// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The exception that is thrown by the marshaler when it encounters a <see cref="MarshalAsAttribute" /> it does not support.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MarshalDirectiveException : SystemException
    {
        public MarshalDirectiveException()
            : base(SR.Arg_MarshalDirectiveException)
        {
            HResult = HResults.COR_E_MARSHALDIRECTIVE;
        }

        public MarshalDirectiveException(string? message)
            : base(message ?? SR.Arg_MarshalDirectiveException)
        {
            HResult = HResults.COR_E_MARSHALDIRECTIVE;
        }

        public MarshalDirectiveException(string? message, Exception? inner)
            : base(message ?? SR.Arg_MarshalDirectiveException, inner)
        {
            HResult = HResults.COR_E_MARSHALDIRECTIVE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MarshalDirectiveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
