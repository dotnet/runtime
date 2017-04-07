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


using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    // 
    [Serializable]
    public class ExternalException : SystemException
    {
        public ExternalException()
            : base(SR.Arg_ExternalException)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(String message)
            : base(message)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(String message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected ExternalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public virtual int ErrorCode
        {
            get
            {
                return HResult;
            }
        }

        public override String ToString()
        {
            String message = Message;
            String s;
            String _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(String.IsNullOrEmpty(message)))
            {
                s = s + ": " + message;
            }

            Exception _innerException = InnerException;

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString();
            }


            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
    }
}
