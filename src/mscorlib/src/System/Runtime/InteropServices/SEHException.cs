// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for all Structured Exception Handling code.
**
**
=============================================================================*/

using System.Runtime.InteropServices;
using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    // Exception for Structured Exception Handler exceptions.
    // 
    public class SEHException : ExternalException
    {
        public SEHException()
            : base()
        {
            HResult = __HResults.E_FAIL;
        }

        public SEHException(String message)
            : base(message)
        {
            HResult = __HResults.E_FAIL;
        }

        public SEHException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.E_FAIL;
        }

        protected SEHException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        // Exceptions can be resumable, meaning a filtered exception 
        // handler can correct the problem that caused the exception,
        // and the code will continue from the point that threw the 
        // exception.
        // 
        // Resumable exceptions aren't implemented in this version,
        // but this method exists and always returns false.
        // 
        public virtual bool CanResume()
        {
            return false;
        }
    }
}
