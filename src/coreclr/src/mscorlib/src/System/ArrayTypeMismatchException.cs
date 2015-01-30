// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: The arrays are of different primitive types.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
    // The ArrayMismatchException is thrown when an attempt to store
    // an object of the wrong type within an array occurs.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ArrayTypeMismatchException : SystemException {
        
        // Creates a new ArrayMismatchException with its message string set to
        // the empty string, its HRESULT set to COR_E_ARRAYTYPEMISMATCH, 
        // and its ExceptionInfo reference set to null. 
        public ArrayTypeMismatchException() 
            : base(Environment.GetResourceString("Arg_ArrayTypeMismatchException")) {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }
        
        // Creates a new ArrayMismatchException with its message string set to
        // message, its HRESULT set to COR_E_ARRAYTYPEMISMATCH, 
        // and its ExceptionInfo reference set to null. 
        // 
        public ArrayTypeMismatchException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }
        
        public ArrayTypeMismatchException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        protected ArrayTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
