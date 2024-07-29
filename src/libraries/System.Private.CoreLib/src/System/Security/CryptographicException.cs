// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Cryptography
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CryptographicException : SystemException
    {
        public CryptographicException()
            : base(SR.Arg_CryptographyException)
        {
        }

        public CryptographicException(int hr)
            : base(SR.Arg_CryptographyException)
        {
            HResult = hr;
        }

        public CryptographicException(string? message)
            : base(message ?? SR.Arg_CryptographyException)
        {
        }

        public CryptographicException(string? message, Exception? inner)
            : base(message ?? SR.Arg_CryptographyException, inner)
        {
        }

        public CryptographicException([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, string? insert)
            : base(string.Format(format, insert))
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected CryptographicException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
