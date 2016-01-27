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
