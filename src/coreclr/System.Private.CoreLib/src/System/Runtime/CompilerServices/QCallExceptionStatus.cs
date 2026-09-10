// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    [NativeMarshalling(typeof(QCallExceptionStatusMarshaller))]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct QCallExceptionStatus
    {
        private readonly nint _value;
    }

    [CustomMarshaller(typeof(QCallExceptionStatus), MarshalMode.Default, typeof(QCallExceptionStatusMarshaller))]
    internal static class QCallExceptionStatusMarshaller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint ConvertToUnmanaged(QCallExceptionStatus _) => 0;

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QCallExceptionStatus ConvertToManaged(nint status)
        {
            if (status != 0)
            {
                HandleException(status);
            }

            return default;
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HandleException(nint status)
        {
            if ((nuint)status <= 2)
            {
                throw Thread.GetQCallSpecialException(status);
            }

            Exception exception;
            GCHandle exceptionHandle = GCHandle.FromIntPtr(status);
            try
            {
                exception = (Exception)exceptionHandle.Target!;
            }
            finally
            {
                exceptionHandle.Free();
            }

            // Throw during unmarshalling so QCall exception propagation remains as close as
            // possible to throwing directly from native code, as QCalls did previously.
            ExceptionDispatchInfo.Throw(exception);
        }
    }
}
