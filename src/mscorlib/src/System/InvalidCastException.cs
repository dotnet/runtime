// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for bad cast conditions!
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class InvalidCastException : SystemException {
        public InvalidCastException() 
            : base(Environment.GetResourceString("Arg_InvalidCastException")) {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }
    
        public InvalidCastException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }

        public InvalidCastException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }

        protected InvalidCastException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public InvalidCastException(String message, int errorCode) 
            : base(message) {
            SetErrorCode(errorCode);
        }

    }

}
