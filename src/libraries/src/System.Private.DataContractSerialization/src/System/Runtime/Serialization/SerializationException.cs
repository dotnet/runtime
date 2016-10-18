// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Serialization
{
    public class SerializationException : SystemException
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
