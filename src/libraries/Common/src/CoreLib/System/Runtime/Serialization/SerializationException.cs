// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Runtime.Serialization
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SerializationException : SystemException
    {
        private static String s_nullMessage = SR.SerializationException;

        // Creates a new SerializationException with its message 
        // string set to a default message.
        public SerializationException()
            : base(s_nullMessage)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        public SerializationException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        public SerializationException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        protected SerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
