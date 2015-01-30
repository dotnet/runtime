// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for Arthimatic Overflows.
**
**
=============================================================================*/

namespace System {
 
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class OverflowException : ArithmeticException {
        public OverflowException() 
            : base(Environment.GetResourceString("Arg_OverflowException")) {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }
    
        public OverflowException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }
        
        public OverflowException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }

        protected OverflowException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
