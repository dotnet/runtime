// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for Timeout
**
**
=============================================================================*/

namespace System 
{
    using System.Runtime.Serialization;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class TimeoutException : SystemException {
        
        public TimeoutException() 
            : base(Environment.GetResourceString("Arg_TimeoutException")) {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }
    
        public TimeoutException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }
        
        public TimeoutException(String message, Exception innerException)
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }
    
        //
        //This constructor is required for serialization.
        //
        protected TimeoutException(SerializationInfo info, StreamingContext context) 
            : base(info, context) {
        }
    }
}

