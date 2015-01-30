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
    [Serializable] public class FieldAccessException : MemberAccessException {
        public FieldAccessException() 
            : base(Environment.GetResourceString("Arg_FieldAccessException")) {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }
    
        public FieldAccessException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }
    
        public FieldAccessException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }

        protected FieldAccessException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
