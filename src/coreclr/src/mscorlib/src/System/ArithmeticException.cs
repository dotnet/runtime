// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for bad arithmetic conditions!
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
    // The ArithmeticException is thrown when overflow or underflow
    // occurs.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class ArithmeticException : SystemException
    {        
        // Creates a new ArithmeticException with its message string set to
        // the empty string, its HRESULT set to COR_E_ARITHMETIC, 
        // and its ExceptionInfo reference set to null. 
        public ArithmeticException() 
            : base(Environment.GetResourceString("Arg_ArithmeticException")) {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }
        
        // Creates a new ArithmeticException with its message string set to
        // message, its HRESULT set to COR_E_ARITHMETIC, 
        // and its ExceptionInfo reference set to null. 
        // 
        public ArithmeticException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }
        
        public ArithmeticException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }

        protected ArithmeticException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
