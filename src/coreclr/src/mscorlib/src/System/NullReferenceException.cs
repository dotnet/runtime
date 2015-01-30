// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for dereferencing a null reference.
**
**
=============================================================================*/

namespace System {   
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class NullReferenceException : SystemException {
        public NullReferenceException() 
            : base(Environment.GetResourceString("Arg_NullReferenceException")) {
            SetErrorCode(__HResults.COR_E_NULLREFERENCE);
        }
    
        public NullReferenceException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_NULLREFERENCE);
        }
        
        public NullReferenceException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_NULLREFERENCE);
        }

        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context) {}

    }

}
