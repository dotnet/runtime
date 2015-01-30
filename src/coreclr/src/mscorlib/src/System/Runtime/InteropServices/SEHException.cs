// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for all Structured Exception Handling code.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System.Runtime.InteropServices;
    using System;
    using System.Runtime.Serialization;
    // Exception for Structured Exception Handler exceptions.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class SEHException : ExternalException {
        public SEHException() 
            : base() {
            SetErrorCode(__HResults.E_FAIL);
        }
        
        public SEHException(String message) 
            : base(message) {
            SetErrorCode(__HResults.E_FAIL);
        }
        
        public SEHException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.E_FAIL);
        }
        
        protected SEHException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        // Exceptions can be resumable, meaning a filtered exception 
        // handler can correct the problem that caused the exception,
        // and the code will continue from the point that threw the 
        // exception.
        // 
        // Resumable exceptions aren't implemented in this version,
        // but this method exists and always returns false.
        // 
        public virtual bool CanResume()
        {
            return false;
        }    
    }
}
