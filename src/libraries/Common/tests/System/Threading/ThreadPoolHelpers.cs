// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal static class ThreadPoolHelpers
    {
        internal static ThreadCountReset EnsureMinThreadsAtLeast(int minWorkerThreads)
        {
            ThreadPool.GetMinThreads(out int workerThreads, out int ioThreads);
            if (workerThreads < minWorkerThreads)
            {
                ThreadPool.SetMinThreads(minWorkerThreads, ioThreads);
                return new ThreadCountReset(workerThreads, ioThreads);
            }

            return default;
        }

        internal struct ThreadCountReset : IDisposable
        {
            private readonly bool _reset;
            private readonly int _worker, _io;

            internal ThreadCountReset(int worker, int io)
            {
                _reset = true;
                _worker = worker;
                _io = io;
            }

            public void Dispose()
            {
                if (_reset)
                {
                    ThreadPool.SetMinThreads(_worker, _io);
                }
            }
        }
    }
}
