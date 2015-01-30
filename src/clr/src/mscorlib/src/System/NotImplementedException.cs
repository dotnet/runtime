// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception thrown when a requested method or operation is not 
**            implemented.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class NotImplementedException : SystemException
    {
        public NotImplementedException() 
            : base(Environment.GetResourceString("Arg_NotImplementedException")) {
            SetErrorCode(__HResults.E_NOTIMPL);
        }
        public NotImplementedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.E_NOTIMPL);
        }
        public NotImplementedException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.E_NOTIMPL);
        }

        protected NotImplementedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
