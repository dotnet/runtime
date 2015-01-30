// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

namespace System.Security {
    using System.Security;
    using System;
    using System.Runtime.Serialization;

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class VerificationException : SystemException {
        public VerificationException() 
            : base(Environment.GetResourceString("Verification_Exception")) {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        protected VerificationException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
