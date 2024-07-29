// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class VerificationException : SystemException
    {
        public VerificationException()
            : base(SR.Verification_Exception)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string? message)
            : base(message ?? SR.Verification_Exception)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string? message, Exception? innerException)
            : base(message ?? SR.Verification_Exception, innerException)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected VerificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
