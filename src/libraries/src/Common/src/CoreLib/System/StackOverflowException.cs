// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for stack overflow.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class StackOverflowException : SystemException
    {
        public StackOverflowException()
            : base(SR.Arg_StackOverflowException)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_STACKOVERFLOW;
        }

        internal StackOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
