// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: An exception class to indicate that the thread was interrupted
**          from a waiting state.
**
**
=============================================================================*/
namespace System.Threading {
    using System.Threading;
    using System;
    using System.Runtime.Serialization;

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ThreadInterruptedException : SystemException {
        public ThreadInterruptedException() 
            : base(GetMessageFromNativeResources(ExceptionMessageKind.ThreadInterrupted)) {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }
        
        public ThreadInterruptedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }
    
        public ThreadInterruptedException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }

        protected ThreadInterruptedException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
