// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    /// <summary>
    /// Overlapped subclass adding data needed by ThreadPoolBoundHandle.
    /// </summary>
    internal sealed class ThreadPoolBoundHandleOverlapped : Overlapped
    {
        private static readonly unsafe IOCompletionCallback s_completionCallback = CompletionCallback;

        private readonly IOCompletionCallback _userCallback;
        internal readonly object _userState;
        internal PreAllocatedOverlapped _preAllocated;
        internal unsafe NativeOverlapped* _nativeOverlapped;
        internal ThreadPoolBoundHandle _boundHandle;
        internal bool _completed;

        public unsafe ThreadPoolBoundHandleOverlapped(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated)
        {
            _userCallback = callback;
            _userState = state;
            _preAllocated = preAllocated;

            _nativeOverlapped = Pack(s_completionCallback, pinData);
            _nativeOverlapped->OffsetLow = 0;        // CLR reuses NativeOverlapped instances and does not reset these
            _nativeOverlapped->OffsetHigh = 0;
        }

        private static unsafe void CompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            ThreadPoolBoundHandleOverlapped overlapped = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(nativeOverlapped);

            //
            // The Win32 thread pool implementation of ThreadPoolBoundHandle does not permit reuse of NativeOverlapped
            // pointers without freeing them and allocating new a new one.  We need to ensure that code using the CLR
            // ThreadPool implementation follows those rules.
            //
            if (overlapped._completed)
                throw new InvalidOperationException(SR.InvalidOperation_NativeOverlappedReused);

            overlapped._completed = true;

            if (overlapped._boundHandle == null)
                throw new InvalidOperationException(SR.Argument_NativeOverlappedAlreadyFree);

            overlapped._userCallback(errorCode, numBytes, nativeOverlapped);
        }
    }
}
