// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class VerificationException : SystemException
    {
        public VerificationException()
            : base(SR.Verification_Exception)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_VERIFICATION;
        }

        protected VerificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
