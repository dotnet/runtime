// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for Arthimatic Overflows.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    public class OverflowException : ArithmeticException
    {
        public OverflowException()
            : base(SR.Arg_OverflowException)
        {
            HResult = __HResults.COR_E_OVERFLOW;
        }

        public OverflowException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_OVERFLOW;
        }

        public OverflowException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_OVERFLOW;
        }

        protected OverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
