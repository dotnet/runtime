// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    [NativeMarshalling(typeof(QCallExceptionMarshaller))]
    internal readonly struct QCallException
    {
    }

    [CustomMarshaller(typeof(QCallException), MarshalMode.ManagedToUnmanagedOut, typeof(QCallExceptionMarshaller))]
    internal struct QCallExceptionMarshaller
    {
        private readonly Thread _thread;

        public QCallExceptionMarshaller()
        {
            _thread = Thread.CurrentThread;
        }

        public void FromUnmanaged(int exceptionPending)
        {
            Debug.Assert(exceptionPending is 0 or 1);

            if (exceptionPending == 1)
            {
                Exception exception = _thread.GetAndClearQCallException();

                // Throw during unmarshalling so QCall exception propagation remains as close as
                // possible to throwing directly from native code, as QCalls did previously.
                ExceptionDispatchInfo.Throw(exception);
            }
        }

        public QCallException ToManaged() => default;

        public void Free()
        {
        }
    }
}
