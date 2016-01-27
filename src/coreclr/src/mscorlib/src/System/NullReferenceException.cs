// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
