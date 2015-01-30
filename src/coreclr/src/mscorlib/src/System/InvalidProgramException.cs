// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: The exception class for programs with invalid IL or bad metadata.
**
**
=============================================================================*/

namespace System {

    using System;
    using System.Runtime.Serialization;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class InvalidProgramException : SystemException {
        public InvalidProgramException() 
            : base(Environment.GetResourceString("InvalidProgram_Default")) {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }
    
        public InvalidProgramException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }
    
        public InvalidProgramException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }

        internal InvalidProgramException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
