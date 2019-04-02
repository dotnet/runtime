// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: An exception class which is thrown into a thread to cause it to
**          abort. This is a special non-catchable exception and results in
**            the thread's death.  This is thrown by the VM only and can NOT be
**          thrown by any user thread, and subclassing this is useless.
**
**
=============================================================================*/

#nullable enable
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ThreadAbortException : SystemException
    {
        internal ThreadAbortException()
        {
            HResult = HResults.COR_E_THREADABORTED;
        }

        public object? ExceptionState => null;

        internal ThreadAbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
