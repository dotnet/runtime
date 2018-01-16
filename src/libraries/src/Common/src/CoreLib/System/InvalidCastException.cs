// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Purpose: Exception class for invalid cast conditions!
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidCastException : SystemException
    {
        public InvalidCastException()
            : base(SR.Arg_InvalidCastException)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_INVALIDCAST;
        }

        public InvalidCastException(String message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected InvalidCastException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
