// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for some failed P/Invoke calls.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class EntryPointNotFoundException : TypeLoadException {
        public EntryPointNotFoundException() 
            : base(Environment.GetResourceString("Arg_EntryPointNotFoundException")) {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }
    
        public EntryPointNotFoundException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }
    
        public EntryPointNotFoundException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }

        protected EntryPointNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    
    
    }

}
