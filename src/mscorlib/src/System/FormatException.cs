// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Exception to designate an illegal argument to FormatMessage.
**
** 
===========================================================*/
namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class FormatException : SystemException {
        public FormatException() 
            : base(Environment.GetResourceString("Arg_FormatException")) {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }
    
        public FormatException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }
        
        public FormatException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }

        protected FormatException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
