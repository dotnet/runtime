// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for dereferencing a null reference.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    public class NullReferenceException : SystemException
    {
        public NullReferenceException()
            : base(SR.Arg_NullReferenceException)
        {
            HResult = __HResults.COR_E_NULLREFERENCE;
        }

        public NullReferenceException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_NULLREFERENCE;
        }

        public NullReferenceException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_NULLREFERENCE;
        }

        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
