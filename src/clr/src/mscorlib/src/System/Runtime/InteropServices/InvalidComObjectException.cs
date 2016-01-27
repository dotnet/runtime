// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: This exception is thrown when an invalid COM object is used. This
**            happens when a the __ComObject type is used directly without
**            having a backing class factory.
**
=============================================================================*/

namespace System.Runtime.InteropServices {

    using System;
    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class InvalidComObjectException : SystemException {
        public InvalidComObjectException() 
            : base(Environment.GetResourceString("Arg_InvalidComObjectException")) {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }
    
        public InvalidComObjectException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }
    
        public InvalidComObjectException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }

        protected InvalidComObjectException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
