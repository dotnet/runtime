// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security.Cryptography
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CryptographicUnexpectedOperationException : CryptographicException
    {
        public CryptographicUnexpectedOperationException()
            : base(SR.Arg_CryptographyException)
        {
        }

        public CryptographicUnexpectedOperationException(string? message)
            : base(message ?? SR.Arg_CryptographyException)
        {
        }

        public CryptographicUnexpectedOperationException(string? message, Exception? inner)
            : base(message ?? SR.Arg_CryptographyException, inner)
        {
        }

        public CryptographicUnexpectedOperationException(string format, string? insert)
            : base(string.Format(CultureInfo.CurrentCulture, format, insert))
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected CryptographicUnexpectedOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
