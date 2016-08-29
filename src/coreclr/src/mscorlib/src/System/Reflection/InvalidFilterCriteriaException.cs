// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// InvalidFilterCriteriaException is thrown in FindMembers when the
// 
//    filter criteria is not valid for the type of filter being used. 
//
// 
//  
//
namespace System.Reflection {

    using System;
    using System.Runtime.Serialization;
    using ApplicationException = System.ApplicationException;
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_CORECLR
    public class InvalidFilterCriteriaException : Exception {
#else
    public class InvalidFilterCriteriaException : ApplicationException {
#endif // FEATURE_CORECLR    
        public InvalidFilterCriteriaException()
            : base(Environment.GetResourceString("Arg_InvalidFilterCriteriaException")) {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }
    
        public InvalidFilterCriteriaException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }
        
        public InvalidFilterCriteriaException(String message, Exception inner) : base(message, inner) {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }

        protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
