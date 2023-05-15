// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.ServiceProcess
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TimeoutException : SystemException
    {
        private const int ServiceControllerTimeout = unchecked((int)0x80131906);

        public TimeoutException()
        {
            HResult = ServiceControllerTimeout;
        }

        public TimeoutException(string? message) : base(message)
        {
            HResult = ServiceControllerTimeout;
        }

        public TimeoutException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = ServiceControllerTimeout;
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
