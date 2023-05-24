// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal struct ThreadPoolCallbackWrapper
    {
        private Thread _currentThread;

        public static ThreadPoolCallbackWrapper Enter()
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                Thread.CurrentThread.IsThreadPoolThread = true;
            }
            return new ThreadPoolCallbackWrapper
            {
                _currentThread = Thread.CurrentThread,
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
