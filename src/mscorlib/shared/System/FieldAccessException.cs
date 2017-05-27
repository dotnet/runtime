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
    public class FieldAccessException : MemberAccessException
    {
        public FieldAccessException()
            : base(SR.Arg_FieldAccessException)
        {
            HResult = __HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_FIELDACCESS;
        }

        protected FieldAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
