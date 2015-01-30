// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// TargetException is thrown when the target to an Invoke is invalid.  This may
// 
//    occur because the caller doesn't have access to the member, or the target doesn't
//    define the member, etc.
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
    public class TargetException : Exception {
#else
    public class TargetException : ApplicationException {
#endif //FEATURE_CORECLR
        public TargetException() : base() {
            SetErrorCode(__HResults.COR_E_TARGET);
        }
    
        public TargetException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_TARGET);
        }
        
        public TargetException(String message, Exception inner) : base(message, inner) {
            SetErrorCode(__HResults.COR_E_TARGET);
        }

        protected TargetException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
