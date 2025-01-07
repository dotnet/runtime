// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when the time allotted for a process or operation has expired.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TimeoutException : SystemException
    {
        public TimeoutException()
            : base(SR.Arg_TimeoutException)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        public TimeoutException(string? message)
            : base(message ?? SR.Arg_TimeoutException)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        public TimeoutException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_TimeoutException, innerException)
        {
            HResult = HResults.COR_E_TIMEOUT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
