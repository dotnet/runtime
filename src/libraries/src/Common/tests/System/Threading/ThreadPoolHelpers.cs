// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Threading
{
    internal static class ThreadPoolHelpers
    {
        // Until ThreadPool.Get/SetMinThreads are exposed, we try to access them via reflection. 

        private static readonly Type _threadPool = typeof(object).GetTypeInfo().Assembly.GetType("System.Threading.ThreadPool");
        private static readonly MethodInfo _getMinThreads = _threadPool?.GetTypeInfo().GetMethod("GetMinThreads");
        private static readonly MethodInfo _setMinThreads = _threadPool?.GetTypeInfo().GetMethod("SetMinThreads");

        internal static ThreadCountReset EnsureMinThreadsAtLeast(int minWorkerThreads)
        {
            if (_getMinThreads != null && _setMinThreads != null)
            {
                var threadCounts = new object[2];
                _getMinThreads.Invoke(null, threadCounts);

                int workerThreads = (int)threadCounts[0];
                if (workerThreads < minWorkerThreads)
                {
                    threadCounts[0] = minWorkerThreads;
                    _setMinThreads.Invoke(null, threadCounts);

                    return new ThreadCountReset(workerThreads, (int)threadCounts[1]);
                }
            }

            return default(ThreadCountReset);
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
                    _setMinThreads?.Invoke(null, new object[] { _worker, _io });
                }
            }
        }
    }
}
