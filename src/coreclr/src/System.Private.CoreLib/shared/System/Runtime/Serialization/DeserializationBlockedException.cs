// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    // Thrown when a dangerous action would be performed during deserialization 
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class DeserializationBlockedException : SerializationException
    {
        // Creates a new DeserializationBlockedException with its message 
        // string set to a default message.
        public DeserializationBlockedException()
            : base(SR.Serialization_DangerousDeserialization)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        // Creates a new DeserializationBlockedException with a message indicating an opt-out switch
        // for a particular part of SerializationGuard
        public DeserializationBlockedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        public DeserializationBlockedException(Exception? innerException)
            : base(SR.Serialization_DangerousDeserialization, innerException)
        {
            HResult = HResults.COR_E_SERIALIZATION;
        }

        private DeserializationBlockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
