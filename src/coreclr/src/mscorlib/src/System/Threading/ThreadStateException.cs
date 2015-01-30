// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: An exception class to indicate that the Thread class is in an
**          invalid state for the method.
**
**
=============================================================================*/

namespace System.Threading {
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ThreadStateException : SystemException {
        public ThreadStateException() 
            : base(Environment.GetResourceString("Arg_ThreadStateException")) {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }
    
        public ThreadStateException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }
        
        public ThreadStateException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }

        protected ThreadStateException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }

}
