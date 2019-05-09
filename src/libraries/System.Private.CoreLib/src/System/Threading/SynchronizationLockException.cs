// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Wait(), Notify() or NotifyAll() was called from an unsynchronized
**          block of code.
**
**
=============================================================================*/

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

        protected SynchronizationLockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
