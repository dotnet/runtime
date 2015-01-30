// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class MethodAccessException : MemberAccessException {
        public MethodAccessException() 
            : base(Environment.GetResourceString("Arg_MethodAccessException")) {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }
    
        public MethodAccessException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }
    
        public MethodAccessException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }

        protected MethodAccessException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
