// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for stack overflow.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class StackOverflowException : SystemException {
        public StackOverflowException() 
            : base(Environment.GetResourceString("Arg_StackOverflowException")) {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }
    
        public StackOverflowException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }
        
        public StackOverflowException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }

        internal StackOverflowException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
        
    }
}
