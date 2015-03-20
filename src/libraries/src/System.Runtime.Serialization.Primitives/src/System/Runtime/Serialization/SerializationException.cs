// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace System.Runtime.Serialization
{
    public class SerializationException : Exception
    {
        private static string s_nullMessage = SR.SerializationException;

        // Creates a new SerializationException with its message 
        // string set to a default message.
        public SerializationException()
            : base(s_nullMessage)
        {
        }

        public SerializationException(string message)
            : base(message)
        {
        }

        public SerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
