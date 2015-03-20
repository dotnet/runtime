// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Runtime.Serialization
{
    public class SerializationException : Exception
    {
        public SerializationException() { }

        public SerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }

        public SerializationException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }
    }
}
