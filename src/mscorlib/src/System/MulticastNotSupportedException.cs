// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// MulticastNotSupportedException
// This is thrown when you add multiple callbacks to a non-multicast delegate.
////////////////////////////////////////////////////////////////////////////////

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class MulticastNotSupportedException : SystemException {
        
        public MulticastNotSupportedException() 
            : base(Environment.GetResourceString("Arg_MulticastNotSupportedException")) {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }
    
        public MulticastNotSupportedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }
        
        public MulticastNotSupportedException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }

        internal MulticastNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
