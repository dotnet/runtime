// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Formats.Cbor
{
    [Serializable]
    public class CborContentException : Exception
    {
        public CborContentException(string? message)
            : base(message ?? SR.CborContentException_DefaultMessage)
        {

        }

        public CborContentException(string? message, Exception? inner)
            : base(message ?? SR.CborContentException_DefaultMessage, inner)
        {

        }

        protected CborContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
