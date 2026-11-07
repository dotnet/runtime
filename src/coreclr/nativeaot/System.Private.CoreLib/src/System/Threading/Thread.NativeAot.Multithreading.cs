// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // Protects starting the thread and setting its priority
        private Lock _lock = new Lock(useTrivialWaits: true);

        // so far the only place we initialize it is `WaitForForegroundThreads`
        // and only in the case when there are running foreground threads
        // by the moment of `StartupCodeHelpers.Shutdown()` invocation
        private static ManualResetEvent s_allDone;

        private static int s_foregroundRunningCount;

        private StartHelper? _startHelper;
        private Exception? _startException;

        static unsafe partial void RegisterThreadExitCallback()
        {
            RuntimeImports.RhSetThreadExitCallback(&OnThreadExit);
        }

        partial void StartCore()
        {
            using (_lock.EnterScope())
            {
                if (!GetThreadStateBit(ThreadState.Unstarted))
                {
                    throw new ThreadStateException(SR.ThreadState_AlreadyStarted);
                }

                bool waitingForThreadStart = false;
                GCHandle<Thread> threadHandle = new GCHandle<Thread>(this);

                try
                {
                    if (!CreateThread(threadHandle))
                    {
                        throw new OutOfMemoryException();
                    }

                    // Skip cleanup if any asynchronous exception happens while waiting for the thread start
                    waitingForThreadStart = true;

                    // Wait until the new thread either dies or reports itself as started
                    while (GetThreadStateBit(ThreadState.Unstarted) && !JoinInternal(0))
                    {
                        Yield();
                    }

                    waitingForThreadStart = false;
                }
                finally
                {
                    Debug.Assert(!waitingForThreadStart, "Leaked threadHandle");
                    if (!waitingForThreadStart)
                    {
                        threadHandle.Dispose();
                    }
                }

                if (GetThreadStateBit(ThreadState.Unstarted))
                {
                    Exception? startException = _startException;
                    _startException = null;

                    throw new ThreadStartException(startException ?? new OutOfMemoryException());
                }

                Debug.Assert(_startException == null);
            }
        }

        private static void StartThread(IntPtr parameter)
        {
            Thread thread = GCHandle<Thread>.FromIntPtr(parameter).Target;

            try
            {
                t_currentThread = thread;
                System.Threading.ManagedThreadId.SetForCurrentThread(thread._managedThreadId);
                thread.InitializeComOnNewThread();
            }
            catch (Exception e)
            {
                thread._startException = e;

#if !TARGET_WINDOWS
                // This should go away once OnThreadExit stops using t_currentThread to signal
                // shutdown of the thread on Unix.
                thread._stopped!.Set();
#endif
                // Terminate the current thread. The creator thread will throw a ThreadStartException.
                return;
            }

            // Report success to the creator thread, which will free threadHandle
            int state = thread.ClearThreadStateBit(ThreadState.Unstarted);
            if ((state & (int)ThreadState.Background) == 0)
            {
                IncrementRunningForeground();
            }

            try
            {
                StartHelper? startHelper = thread._startHelper;
                Debug.Assert(startHelper is not null);
                thread._startHelper = null;

                startHelper.Run();
            }
            catch (Exception ex) when (ExceptionHandling.IsHandledByGlobalHandler(ex))
            {
                // the handler returned "true" means the exception is now "handled" and we should gracefully exit.
            }
            finally
            {
                thread.SetThreadStateBit(ThreadState.Stopped);
            }
        }

        private static void StopThread(Thread thread)
        {
            if ((thread._threadState & (int)(ThreadState.Stopped | ThreadState.Aborted)) == 0)
            {
                thread.SetThreadStateBit(ThreadState.Stopped);
            }

            int state = thread.ClearThreadStateBit(ThreadState.Background);
            if ((state & (int)ThreadState.Background) == 0)
            {
                DecrementRunningForeground();
            }
        }

        internal static void IncrementRunningForeground()
        {
            Interlocked.Increment(ref s_foregroundRunningCount);
        }

        internal static void DecrementRunningForeground()
        {
            if (Interlocked.Decrement(ref s_foregroundRunningCount) == 0)
            {
                // Interlocked.Decrement issues full memory barrier
                // so most recent write to s_allDone should be visible here
                s_allDone?.Set();
            }
        }

        internal static void WaitForForegroundThreads()
        {
            Thread.CurrentThread.IsBackground = true;
            // last read/write inside `IsBackground` issues full barrier no matter of logic flow
            // so we can just read `s_foregroundRunningCount`
            if (s_foregroundRunningCount == 0)
            {
                // current thread is the last foreground thread, so let the runtime finish
                return;
            }
            Volatile.Write(ref s_allDone, new ManualResetEvent(false));
            // other foreground threads could have their job finished meanwhile
            // Volatile.Write above issues release barrier
            // but we need acquire barrier to observe most recent write to s_foregroundRunningCount
            if (Volatile.Read(ref s_foregroundRunningCount) == 0)
            {
                return;
            }
            s_allDone.WaitOne();
        }
    }
}
