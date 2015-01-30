// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: The exception class for running out of memory
** but most likely in a non-fatal way that shouldn't 
** be affected by escalation policy.  Use this for cases
** like MemoryFailPoint or a TryAllocate method, where you 
** expect OOM's with no shared state corruption and you
** want to recover from these errors.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class InsufficientMemoryException : OutOfMemoryException
    {
        public InsufficientMemoryException() 
            : base(GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory)) {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }
    
        public InsufficientMemoryException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }
        
        public InsufficientMemoryException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }

        private InsufficientMemoryException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
