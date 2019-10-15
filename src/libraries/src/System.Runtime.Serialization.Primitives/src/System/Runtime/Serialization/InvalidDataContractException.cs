// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
    [Serializable]
    [TypeForwardedFrom("System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidDataContractException : Exception
    {
        public InvalidDataContractException()
            : base()
        {
        }

        public InvalidDataContractException(string message)
            : base(message)
        {
        }

        public InvalidDataContractException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidDataContractException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
