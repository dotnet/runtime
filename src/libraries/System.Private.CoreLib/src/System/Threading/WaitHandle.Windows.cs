// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
        private static unsafe int WaitMultipleIgnoringSyncContextCore(ReadOnlySpan<IntPtr> handles, bool waitAll, int millisecondsTimeout)
        {
            fixed (IntPtr* pHandles = &MemoryMarshal.GetReference(handles))
            {
                return WaitForMultipleObjectsIgnoringSyncContext(pHandles, handles.Length, waitAll, millisecondsTimeout, useTrivialWaits: false);
            }
        }

        private static unsafe int WaitForMultipleObjectsIgnoringSyncContext(IntPtr* pHandles, int numHandles, bool waitAll, int millisecondsTimeout, bool useTrivialWaits)
        {
            Debug.Assert(millisecondsTimeout >= -1);

            // Normalize waitAll
            if (numHandles == 1)
                waitAll = false;

#if !MONO // TODO: reentrant wait support in Mono https://github.com/dotnet/runtime/issues/49518
            // Trivial waits don't allow reentrance
            bool reentrantWait = !useTrivialWaits && Thread.ReentrantWaitsEnabled;

            if (reentrantWait)
            {
                //
                // In the CLR, we use CoWaitForMultipleHandles to pump messages while waiting in an STA.  In that case, we cannot use WAIT_ALL.
                // That's because the wait would only be satisfied if a message arrives while the handles are signalled.
                //
                if (waitAll)
                    throw new NotSupportedException(SR.NotSupported_WaitAllSTAThread);

                // CoWaitForMultipleHandles does not support more than 63 handles. It returns RPC_S_CALLPENDING for more than 63 handles
                // that is impossible to differentiate from timeout.
                if (numHandles > 63)
                    throw new NotSupportedException(SR.NotSupported_MaxWaitHandles_STA);
            }
#endif

            Thread currentThread = Thread.CurrentThread;
            currentThread.SetWaitSleepJoinState();

            long startTime = 0;
            if (millisecondsTimeout != -1)
            {
                startTime = Environment.TickCount64;
            }

            int result;

            Thread.CheckForPendingInterrupt();

            while (true)
            {
#if !MONO
                if (reentrantWait)
                {
                    Debug.Assert(!waitAll);
                    result = Thread.ReentrantWaitAny(true, millisecondsTimeout, numHandles, pHandles);
                }
                else
                {
                    result = (int)Interop.Kernel32.WaitForMultipleObjectsEx((uint)numHandles, (IntPtr)pHandles, waitAll ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, (uint)millisecondsTimeout, Interop.BOOL.TRUE);
                }
#else
                result = (int)Interop.Kernel32.WaitForMultipleObjectsEx((uint)numHandles, (IntPtr)pHandles, waitAll ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, (uint)millisecondsTimeout, Interop.BOOL.TRUE);
#endif

                if (result != Interop.Kernel32.WAIT_IO_COMPLETION)
                    break;

                Thread.CheckForPendingInterrupt();

                // Handle APC completion by adjusting timeout and retrying
                if (millisecondsTimeout != Timeout.Infinite)
                {
                    long currentTime = Environment.TickCount64;
                    long elapsed = currentTime - startTime;
                    if (elapsed >= millisecondsTimeout)
                    {
                        result = Interop.Kernel32.WAIT_TIMEOUT;
                        break;
                    }
                    millisecondsTimeout -= (int)elapsed;
                    startTime = currentTime;
                }
            }

            currentThread.ClearWaitSleepJoinState();

            if (result == Interop.Kernel32.WAIT_FAILED)
            {
                int errorCode = Interop.Kernel32.GetLastError();
                if (waitAll && errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                {
                    // Check for duplicate handles. This is a brute force O(n^2) search, which is intended since the typical
                    // array length is short enough that this would actually be faster than using a hash set. Also, the worst
                    // case is not so bad considering that the array length is limited by
                    // <see cref="WaitHandle.MaxWaitHandles"/>.
                    for (int i = 1; i < numHandles; ++i)
                    {
                        IntPtr handle = pHandles[i];
                        for (int j = 0; j < i; ++j)
                        {
                            if (pHandles[j] == handle)
                            {
                                throw new DuplicateWaitObjectException("waitHandles[" + i + ']');
                            }
                        }
                    }
                }

                ThrowWaitFailedException(errorCode);
            }

            return result;
        }

        internal static unsafe int WaitOneCore(IntPtr handle, int millisecondsTimeout, bool useTrivialWaits)
        {
            return WaitForMultipleObjectsIgnoringSyncContext(&handle, 1, false, millisecondsTimeout, useTrivialWaits);
        }

        private static int SignalAndWaitCore(IntPtr handleToSignal, IntPtr handleToWaitOn, int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= -1);

            long startTime = 0;
            if (millisecondsTimeout != -1)
            {
                startTime = Environment.TickCount64;
            }

            Thread.CheckForPendingInterrupt();

            // Signal the object and wait for the first time
            int ret = (int)Interop.Kernel32.SignalObjectAndWait(handleToSignal, handleToWaitOn, (uint)millisecondsTimeout, Interop.BOOL.TRUE);

            // Handle APC completion by retrying with WaitForSingleObjectEx (without signaling again)
            while (ret == Interop.Kernel32.WAIT_IO_COMPLETION)
            {
                Thread.CheckForPendingInterrupt();

                if (millisecondsTimeout != -1)
                {
                    long currentTime = Environment.TickCount64;
                    long elapsed = currentTime - startTime;

                    if (elapsed >= millisecondsTimeout)
                    {
                        ret = Interop.Kernel32.WAIT_TIMEOUT;
                        break;
                    }
                    millisecondsTimeout -= (int)elapsed;
                    startTime = currentTime;
                }

                // For retries, only wait on the handle (don't signal again)
                ret = (int)Interop.Kernel32.WaitForSingleObjectEx(handleToWaitOn, (uint)millisecondsTimeout, Interop.BOOL.TRUE);
            }
            if (ret == Interop.Kernel32.WAIT_FAILED)
            {
                ThrowWaitFailedException(Interop.Kernel32.GetLastError());
            }

            return ret;
        }

        private static void ThrowWaitFailedException(int errorCode)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_INVALID_HANDLE:
                    ThrowInvalidHandleException();
                    return;

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                    throw new ArgumentException();

                case Interop.Errors.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException();

                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException();

                case Interop.Errors.ERROR_TOO_MANY_POSTS:
                    // Only applicable to <see cref="WaitHandle.SignalAndWait(WaitHandle, WaitHandle)"/>. Note however, that
                    // if the semahpore already has the maximum signal count, the Windows SignalObjectAndWait function does not
                    // return an error, but this code is kept for historical reasons and to convey the intent, since ideally,
                    // that should be an error.
                    throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);

                case Interop.Errors.ERROR_NOT_OWNER:
                    // Only applicable to <see cref="WaitHandle.SignalAndWait(WaitHandle, WaitHandle)"/> when signaling a mutex
                    // that is locked by a different thread. Note that if the mutex is already unlocked, the Windows
                    // SignalObjectAndWait function does not return an error.
                    throw new ApplicationException(SR.Arg_SynchronizationLockException);

                case Interop.Errors.ERROR_MUTANT_LIMIT_EXCEEDED:
                    throw new OverflowException(SR.Overflow_MutexReacquireCount);

                default:
                    throw new Exception { HResult = errorCode };
            }
        }

        internal static Exception ExceptionFromCreationError(int errorCode, string path)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_PATH_NOT_FOUND:
                    return new IOException(SR.Format(SR.IO_PathNotFound_Path, path));

                case Interop.Errors.ERROR_ACCESS_DENIED:
                    return new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path));

                case Interop.Errors.ERROR_ALREADY_EXISTS:
                    return new IOException(SR.Format(SR.IO_AlreadyExists_Name, path));

                case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                    return new PathTooLongException();

                default:
                    return new IOException(SR.Arg_IOException, errorCode);
            }
        }
    }
}
