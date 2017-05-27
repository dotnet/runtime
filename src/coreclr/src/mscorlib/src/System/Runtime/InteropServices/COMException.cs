// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for all errors from COM Interop where we don't
** recognize the HResult.
**
**
=============================================================================*/

using System;
using System.Runtime.Serialization;
using System.Globalization;
using System.Security;
using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
    // Exception for COM Interop errors where we don't recognize the HResult.
    // 
    public class COMException : ExternalException
    {
        public COMException()
            : base(SR.Arg_COMException)
        {
            HResult = __HResults.E_FAIL;
        }

        public COMException(String message)
            : base(message)
        {
            HResult = __HResults.E_FAIL;
        }

        public COMException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.E_FAIL;
        }

        public COMException(String message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected COMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public override String ToString()
        {
            String message = Message;
            String s;
            String _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(message == null || message.Length <= 0))
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
