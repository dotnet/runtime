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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromUnmanaged(int exceptionPending)
        {
            if (exceptionPending == 1)
            {
                HandleException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QCallException ToManaged() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HandleException()
        {
            Exception exception = Thread.GetAndClearQCallException();

            // Throw during unmarshalling so QCall exception propagation remains as close as
            // possible to throwing directly from native code, as QCalls did previously.
            ExceptionDispatchInfo.Throw(exception);
        }
    }
}
