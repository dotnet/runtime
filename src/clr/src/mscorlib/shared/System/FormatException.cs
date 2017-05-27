// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Exception to designate an illegal argument to FormatMessage.
**
** 
===========================================================*/

using System.Runtime.Serialization;

namespace System
{
    public class FormatException : SystemException
    {
        public FormatException()
            : base(SR.Arg_FormatException)
        {
            HResult = __HResults.COR_E_FORMAT;
        }

        public FormatException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_FORMAT;
        }

        public FormatException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_FORMAT;
        }

        protected FormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
