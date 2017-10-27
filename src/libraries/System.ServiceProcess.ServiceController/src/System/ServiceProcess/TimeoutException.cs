// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.ServiceProcess
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TimeoutException : SystemException
    {
        private const int ServiceControllerTimeout = unchecked((int)0x80131906);

        public TimeoutException() : base()
        {
            HResult = ServiceControllerTimeout;
        }

        public TimeoutException(string message) : base(message)
        {
            HResult = ServiceControllerTimeout;
        }

        public TimeoutException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = ServiceControllerTimeout;
        }

        protected TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
