// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading
{
    /// <summary>
    /// The exception that is thrown when a method requires the caller to own the lock on a given Monitor, and the method is invoked by a caller that does not own that lock.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SynchronizationLockException : SystemException
    {
        public SynchronizationLockException()
            : base(SR.Arg_SynchronizationLockException)
        {
            HResult = HResults.COR_E_SYNCHRONIZATIONLOCK;
        }

        public SynchronizationLockException(string? message)
            : base(message ?? SR.Arg_SynchronizationLockException)
        {
            HResult = HResults.COR_E_SYNCHRONIZATIONLOCK;
        }

        public SynchronizationLockException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_SynchronizationLockException, innerException)
        {
            HResult = HResults.COR_E_SYNCHRONIZATIONLOCK;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SynchronizationLockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
