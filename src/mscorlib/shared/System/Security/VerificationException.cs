// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Security
{
    public class VerificationException : SystemException
    {
        public VerificationException()
            : base(SR.Verification_Exception)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        public VerificationException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        protected VerificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
