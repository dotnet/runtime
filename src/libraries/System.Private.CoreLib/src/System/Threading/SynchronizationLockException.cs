// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Wait(), Notify() or NotifyAll() was called from an unsynchronized
**          block of code.
**
**
=============================================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SynchronizationLockException : SystemException
    {
        public SynchronizationLockException()
            : base(SR.Arg_SynchronizationLockException)
        {
            HResult = HResults.COR_E_SYNCHRONIZATIONLOCK;
        }

        public SynchronizationLockException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SYNCHRONIZATIONLOCK;
        }

        public SynchronizationLockException(string? message, Exception? innerException)
            : base(message, innerException)
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
