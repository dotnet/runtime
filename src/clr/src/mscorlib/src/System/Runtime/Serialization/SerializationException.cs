// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Thrown when something goes wrong during serialization or 
**          deserialization.
**
**
=============================================================================*/

namespace System.Runtime.Serialization {
    
    using System;
    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class SerializationException : SystemException {
        
        private static String _nullMessage = Environment.GetResourceString("Arg_SerializationException");
        
        // Creates a new SerializationException with its message 
        // string set to a default message.
        public SerializationException() 
            : base(_nullMessage) {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }
        
        public SerializationException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        public SerializationException(String message, Exception innerException) : base (message, innerException) {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        protected SerializationException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
