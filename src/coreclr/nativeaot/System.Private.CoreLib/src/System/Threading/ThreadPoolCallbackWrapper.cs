// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    /// <summary>
    /// Ensures <c>Thread.CurrentThread</c> is initialized for a callback running on a thread pool thread.
    /// If WinRT is enabled, also ensures the Windows Runtime is initialized during the execution of the callback.
    /// </summary>
    /// <remarks>
    /// This structure does not implement <c>IDisposable</c> to save on exception support, which callers do not need.
    /// </remarks>
    internal struct ThreadPoolCallbackWrapper
    {
        private Thread _currentThread;

        public static ThreadPoolCallbackWrapper Enter()
        {
            return new ThreadPoolCallbackWrapper
            {
                _currentThread = Thread.EnsureThreadPoolThreadInitialized(),
            };
        }

        public void Exit(bool resetThread = true)
        {
            if (resetThread)
            {
                _currentThread.ResetThreadPoolThread();
            }
        }
    }
}
