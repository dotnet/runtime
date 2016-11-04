// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Net.NetworkInformation
{
    [Serializable]
    public class PingException : InvalidOperationException
    {
        public PingException(string message) :
            base(message)
        {
        }

        public PingException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        protected PingException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
