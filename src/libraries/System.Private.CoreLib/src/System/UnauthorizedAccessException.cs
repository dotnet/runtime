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

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    // The UnauthorizedAccessException is thrown when access errors 
    // occur from IO or other OS methods.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class UnauthorizedAccessException : SystemException
    {
        public UnauthorizedAccessException()
            : base(SR.Arg_UnauthorizedAccessException)
        {
            HResult = HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        public UnauthorizedAccessException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        public UnauthorizedAccessException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_UNAUTHORIZEDACCESS;
        }

        protected UnauthorizedAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
