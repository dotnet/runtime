// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for duplicate objects in WaitAll/WaitAny.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;

    // The DuplicateWaitObjectException is thrown when an object 
    // appears more than once in the list of objects to WaitAll or WaitAny.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class DuplicateWaitObjectException : ArgumentException {

        private static volatile String _duplicateWaitObjectMessage = null;

        private static String DuplicateWaitObjectMessage {
            get {
                if (_duplicateWaitObjectMessage == null)
                    _duplicateWaitObjectMessage = Environment.GetResourceString("Arg_DuplicateWaitObjectException");
                return _duplicateWaitObjectMessage;
            }
        }

        // Creates a new DuplicateWaitObjectException with its message 
        // string set to a default message.
        public DuplicateWaitObjectException() 
            : base(DuplicateWaitObjectMessage) {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String parameterName) 
            : base(DuplicateWaitObjectMessage, parameterName) {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String parameterName, String message) 
            : base(message, parameterName) {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        // This constructor is required for serialization
        protected DuplicateWaitObjectException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
