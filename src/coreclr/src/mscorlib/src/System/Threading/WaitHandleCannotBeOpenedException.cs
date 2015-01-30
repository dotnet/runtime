// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
namespace System.Threading 
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    [Serializable]
    [ComVisibleAttribute(false)]

#if FEATURE_CORECLR
    public class WaitHandleCannotBeOpenedException : Exception {
#else
    public class WaitHandleCannotBeOpenedException : ApplicationException { 
#endif // FEATURE_CORECLR
        public WaitHandleCannotBeOpenedException() : base(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException")) 
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }
    
        public WaitHandleCannotBeOpenedException(String message) : base(message)
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }

        public WaitHandleCannotBeOpenedException(String message, Exception innerException) : base(message, innerException)
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }

        protected WaitHandleCannotBeOpenedException(SerializationInfo info, StreamingContext context) : base (info, context) 
        {
        }
    }
}

