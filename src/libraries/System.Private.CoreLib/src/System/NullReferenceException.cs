// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NullReferenceException : SystemException
    {
        public NullReferenceException()
            : base(SR.Arg_NullReferenceException)
        {
            HResult = HResults.E_POINTER;
        }

        public NullReferenceException(string? message)
            : base(message)
        {
            HResult = HResults.E_POINTER;
        }

        public NullReferenceException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_POINTER;
        }

        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
