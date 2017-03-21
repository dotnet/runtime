// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace System.Threading
{
    [Serializable]
    [ComVisibleAttribute(false)]
    public class SemaphoreFullException : SystemException
    {
        public SemaphoreFullException() : base(Environment.GetResourceString("Threading_SemaphoreFullException"))
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

