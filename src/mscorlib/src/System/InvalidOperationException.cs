// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for denoting an object was in a state that
** made calling a method illegal.
**
**
=============================================================================*/
namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class InvalidOperationException : SystemException
    {
        public InvalidOperationException() 
            : base(Environment.GetResourceString("Arg_InvalidOperationException")) {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }
        
        public InvalidOperationException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }
    
        public InvalidOperationException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        protected InvalidOperationException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}

