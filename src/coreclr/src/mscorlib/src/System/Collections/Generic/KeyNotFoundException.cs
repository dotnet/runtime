// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
** 
**
**
** Purpose: Exception class for Hashtable and Dictionary.
**
**
=============================================================================*/


using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    public class KeyNotFoundException : SystemException, ISerializable
    {
        public KeyNotFoundException()
            : base(Environment.GetResourceString("Arg_KeyNotFound"))
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message)
            : base(message)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }


        protected KeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
