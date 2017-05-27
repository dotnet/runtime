// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    public class MethodAccessException : MemberAccessException
    {
        public MethodAccessException()
            : base(SR.Arg_MethodAccessException)
        {
            HResult = __HResults.COR_E_METHODACCESS;
        }

        public MethodAccessException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_METHODACCESS;
        }

        public MethodAccessException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_METHODACCESS;
        }

        protected MethodAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
