// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Threading
{
    /// <summary>
    /// An exception class to indicate that the thread was interrupted from a waiting state.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ThreadInterruptedException : SystemException
    {
        public ThreadInterruptedException() : base(
#if CORECLR
            GetMessageFromNativeResources(ExceptionMessageKind.ThreadInterrupted)
#else
            SR.Threading_ThreadInterrupted
#endif
            )
        {
            HResult = HResults.COR_E_THREADINTERRUPTED;
        }

        public ThreadInterruptedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_THREADINTERRUPTED;
        }

        public ThreadInterruptedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_THREADINTERRUPTED;
        }

        protected ThreadInterruptedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
