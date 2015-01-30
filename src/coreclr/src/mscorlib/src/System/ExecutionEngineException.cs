// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
//
/*=============================================================================
**
**
**
** Purpose: The exception class for misc execution engine exceptions.
**          Currently, its only used as a placeholder type when the EE
**          does a FailFast.
**
**
=============================================================================*/

namespace System {

	using System;
	using System.Runtime.Serialization;
    [Obsolete("This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class ExecutionEngineException : SystemException {
        public ExecutionEngineException() 
            : base(Environment.GetResourceString("Arg_ExecutionEngineException")) {
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }
    
        public ExecutionEngineException(String message) 
            : base(message) {
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }
    
        public ExecutionEngineException(String message, Exception innerException) 
            : base(message, innerException) {
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        internal ExecutionEngineException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
