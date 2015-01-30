// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// TargetInvocationException is used to report an exception that was thrown
// 
//    by the target of an invocation.
//
// 
// 
//
namespace System.Reflection {
    
    
    using System;
    using System.Runtime.Serialization;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_CORECLR
    public sealed class TargetInvocationException : Exception {
#else
    public sealed class TargetInvocationException : ApplicationException {
#endif //FEATURE_CORECLR
        // This exception is not creatable without specifying the
        //    inner exception.
        private TargetInvocationException()
            : base(Environment.GetResourceString("Arg_TargetInvocationException")) {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        // This is called from within the runtime.
        private TargetInvocationException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }       
        
        public TargetInvocationException(System.Exception inner) 
            : base(Environment.GetResourceString("Arg_TargetInvocationException"), inner) {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }
    
        public TargetInvocationException(String message, Exception inner) : base(message, inner) {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        internal TargetInvocationException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
