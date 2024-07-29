// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading
{
    /// <summary>
    /// The exception that is thrown when a <see cref="Thread" /> is in an invalid <see cref="Thread.ThreadState" /> for the method call.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ThreadStateException : SystemException
    {
        public ThreadStateException()
            : base(SR.Arg_ThreadStateException)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        public ThreadStateException(string? message)
            : base(message ?? SR.Arg_ThreadStateException)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        public ThreadStateException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_ThreadStateException, innerException)
        {
            HResult = HResults.COR_E_THREADSTATE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ThreadStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
