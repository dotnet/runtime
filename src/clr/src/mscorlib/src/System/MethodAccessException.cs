// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
