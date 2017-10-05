// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SemaphoreFullException : SystemException
    {
        public SemaphoreFullException() : base(SR.Threading_SemaphoreFullException)
        {
        }

        public SemaphoreFullException(String message) : base(message)
        {
        }

        public SemaphoreFullException(String message, Exception innerException) : base(message, innerException)
        {
        }

        protected SemaphoreFullException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
