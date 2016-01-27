// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: Wait(), Notify() or NotifyAll() was called from an unsynchronized
**          block of code.
**
**
=============================================================================*/

namespace System.Threading {

    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class SynchronizationLockException : SystemException {
        public SynchronizationLockException() 
            : base(Environment.GetResourceString("Arg_SynchronizationLockException")) {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }
    
        public SynchronizationLockException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }
    
        public SynchronizationLockException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }

        protected SynchronizationLockException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }

}


