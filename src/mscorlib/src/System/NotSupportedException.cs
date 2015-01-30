// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: For methods that should be implemented on subclasses.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class NotSupportedException : SystemException
    {
        public NotSupportedException() 
            : base(Environment.GetResourceString("Arg_NotSupportedException")) {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }
    
        public NotSupportedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }
        
        public NotSupportedException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }

        protected NotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
