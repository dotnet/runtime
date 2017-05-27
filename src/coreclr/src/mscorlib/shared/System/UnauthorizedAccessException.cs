// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: An exception for OS 'access denied' types of 
**          errors, including IO and limited security types 
**          of errors.
**
** 
===========================================================*/

using System.Runtime.Serialization;

namespace System
{
    // The UnauthorizedAccessException is thrown when access errors 
    // occur from IO or other OS methods.  
    public class UnauthorizedAccessException : SystemException
    {
        public UnauthorizedAccessException()
            : base(SR.Arg_UnauthorizedAccessException)
        {
            HResult = __HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        public UnauthorizedAccessException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        public UnauthorizedAccessException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        protected UnauthorizedAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
