// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
