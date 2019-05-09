// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class representing an AV that was deemed unsafe and may have corrupted the application.
**
**
=============================================================================*/

using System;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AccessViolationException : SystemException
    {
        public AccessViolationException()
            : base(SR.Arg_AccessViolationException)
        {
            HResult = HResults.E_POINTER;
        }

        public AccessViolationException(string? message)
            : base(message)
        {
            HResult = HResults.E_POINTER;
        }

        public AccessViolationException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_POINTER;
        }

        protected AccessViolationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

#pragma warning disable 169  // Field is not used from managed.
        private IntPtr _ip;             // Address of faulting instruction.
        private IntPtr _target;         // Address that could not be accessed.
        private int _accessType;        // 0:read, 1:write
#pragma warning restore 169
    }
}
