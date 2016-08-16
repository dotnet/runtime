// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for attempting to pass an instance through a context
**          boundary, when the formal type and the instance's marshal style are
**          incompatible or cannot be marshaled.
**
**          This is thrown by the VM when attempts to marshal the exception 
**          object at the AppDomain transition boundary fails.
=============================================================================*/

namespace System {
	using System.Runtime.InteropServices;
	using System.Runtime.Remoting;
	using System;
	using System.Runtime.Serialization;
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ContextMarshalException : SystemException {
        public ContextMarshalException() 
            : base(Environment.GetResourceString("Arg_ContextMarshalException")) {
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }
    
        public ContextMarshalException(String message) 
            : base(message) {
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }
    	
        public ContextMarshalException(String message, Exception inner) 
            : base(message, inner) {
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        protected ContextMarshalException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }

}
