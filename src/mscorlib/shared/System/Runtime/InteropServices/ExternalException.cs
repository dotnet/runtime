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
    public class ExternalException : SystemException
    {
        public ExternalException()
            : base(SR.Arg_ExternalException)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(string message)
            : base(message)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.E_FAIL;
        }

        public ExternalException(string message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected ExternalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual int ErrorCode
        {
            get
            {
                return HResult;
            }
        }

        public override string ToString()
        {
            string message = Message;
            string className = GetType().ToString();

            string s = className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(String.IsNullOrEmpty(message)))
            {
                s = s + ": " + message;
            }

            Exception innerException = InnerException;

            if (innerException != null)
            {
                s = s + " ---> " + innerException.ToString();
            }

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
    }
}
