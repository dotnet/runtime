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

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ExternalException : SystemException
    {
        public ExternalException()
            : base(SR.Arg_ExternalException)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message)
            : base(message)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected ExternalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
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

            if (!string.IsNullOrEmpty(message))
            {
                s = s + ": " + message;
            }

            Exception? innerException = InnerException;
            if (innerException != null)
            {
                s = s + Environment.NewLine + InnerExceptionPrefix + innerException.ToString();
            }

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
    }
}
