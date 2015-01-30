// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** Purpose: This exception is thrown when the runtime rank of a safe array
**            is different than the array rank specified in the metadata.
**
=============================================================================*/

namespace System.Runtime.InteropServices {

    using System;
    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class SafeArrayRankMismatchException : SystemException {
        public SafeArrayRankMismatchException() 
            : base(Environment.GetResourceString("Arg_SafeArrayRankMismatchException")) {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }
    
        public SafeArrayRankMismatchException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }
    
        public SafeArrayRankMismatchException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }

        protected SafeArrayRankMismatchException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
