// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// TargetParameterCountException is thrown when the number of parameter to an
// 
//    invocation doesn't match the number expected.
//
// 
// 
//
namespace System.Reflection {

    using System;
    using SystemException = System.SystemException;
    using System.Runtime.Serialization;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_CORECLR
    public sealed class TargetParameterCountException : Exception {
#else
    public sealed class TargetParameterCountException : ApplicationException {
#endif //FEATURE_CORECLR
        public TargetParameterCountException()
            : base(Environment.GetResourceString("Arg_TargetParameterCountException")) {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }
    
        public TargetParameterCountException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }
        
        public TargetParameterCountException(String message, Exception inner)  
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }

        internal TargetParameterCountException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
