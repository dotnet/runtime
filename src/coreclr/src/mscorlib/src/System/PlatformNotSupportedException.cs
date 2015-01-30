// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: To handle features that don't run on particular platforms
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class PlatformNotSupportedException : NotSupportedException
    {
        public PlatformNotSupportedException() 
            : base(Environment.GetResourceString("Arg_PlatformNotSupported")) {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }
    
        public PlatformNotSupportedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }
        
        public PlatformNotSupportedException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }

        protected PlatformNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
