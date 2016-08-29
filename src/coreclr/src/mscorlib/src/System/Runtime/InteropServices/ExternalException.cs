// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception base class for all errors from Interop or Structured 
**          Exception Handling code.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {

    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    // 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ExternalException : SystemException {
        public ExternalException() 
            : base(Environment.GetResourceString("Arg_ExternalException")) {
            SetErrorCode(__HResults.E_FAIL);
        }
        
        public ExternalException(String message) 
            : base(message) {
            SetErrorCode(__HResults.E_FAIL);
        }
        
        public ExternalException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message,int errorCode) 
            : base(message) {
            SetErrorCode(errorCode);
        }

        protected ExternalException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public virtual int ErrorCode {
            get {
                return HResult;
            }
        }

#if !FEATURE_CORECLR // Breaks the subset-of-Orcas property
        public override String ToString() {
            String message = Message;
            String s;
            String _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(String.IsNullOrEmpty(message))) {
                s = s + ": " + message;
            }

            Exception _innerException = InnerException;

            if (_innerException!=null) {
                s = s + " ---> " + _innerException.ToString();
            }


            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
#endif // !FEATURE_CORECLR
    }
}
