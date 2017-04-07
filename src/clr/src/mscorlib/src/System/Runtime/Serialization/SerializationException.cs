// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Thrown when something goes wrong during serialization or 
**          deserialization.
**
**
=============================================================================*/


using System;
using System.Runtime.Serialization;

namespace System.Runtime.Serialization
{
    [Serializable]
    public class SerializationException : SystemException
    {
        private static String _nullMessage = SR.Arg_SerializationException;

        // Creates a new SerializationException with its message 
        // string set to a default message.
        public SerializationException()
            : base(_nullMessage)
        {
            HResult = __HResults.COR_E_SERIALIZATION;
        }

        public SerializationException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_SERIALIZATION;
        }

        public SerializationException(String message, Exception innerException) : base(message, innerException)
        {
            HResult = __HResults.COR_E_SERIALIZATION;
        }

        protected SerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
