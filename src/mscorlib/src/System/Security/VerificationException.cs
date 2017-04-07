// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System.Security;
using System;
using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    public class VerificationException : SystemException
    {
        public VerificationException()
            : base(SR.Verification_Exception)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        public VerificationException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        public VerificationException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_VERIFICATION;
        }

        protected VerificationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
