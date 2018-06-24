// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The type of an OLE variant that was passed into the runtime is 
**            invalid.
**
=============================================================================*/


using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidOleVariantTypeException : SystemException
    {
        public InvalidOleVariantTypeException()
            : base(SR.Arg_InvalidOleVariantTypeException)
        {
            HResult = HResults.COR_E_INVALIDOLEVARIANTTYPE;
        }

        public InvalidOleVariantTypeException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_INVALIDOLEVARIANTTYPE;
        }

        public InvalidOleVariantTypeException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_INVALIDOLEVARIANTTYPE;
        }

        protected InvalidOleVariantTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
