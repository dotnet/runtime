// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for null arguments to a method.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.Remoting;
    using System.Security.Permissions;
    
    // The ArgumentException is thrown when an argument 
    // is null when it shouldn't be.
    // 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ArgumentNullException : ArgumentException
    {
        // Creates a new ArgumentNullException with its message 
        // string set to a default message explaining an argument was null.
       public ArgumentNullException() 
            : base(Environment.GetResourceString("ArgumentNull_Generic")) {
                // Use E_POINTER - COM used that for null pointers.  Description is "invalid pointer"
                SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String paramName) 
            : base(Environment.GetResourceString("ArgumentNull_Generic"), paramName) {
            SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.E_POINTER);
        }
            
        public ArgumentNullException(String paramName, String message) 
            : base(message, paramName) {
            SetErrorCode(__HResults.E_POINTER);   
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        protected ArgumentNullException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
