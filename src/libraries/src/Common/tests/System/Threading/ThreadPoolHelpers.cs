// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Threading
{
    internal static class ThreadPoolHelpers
    {
        internal static void EnsureMinThreadsAtLeast(int minWorkerThreads)
        {
            // Until ThreadPool.Get/SetMinThreads are exposed, we try to access them via reflection. 

            Type threadPool = typeof(object).GetTypeInfo().Assembly.GetType("System.Threading.ThreadPool");
            MethodInfo getMinThreads = threadPool?.GetTypeInfo().GetMethod("GetMinThreads");
            MethodInfo setMinThreads = threadPool?.GetTypeInfo().GetMethod("SetMinThreads");
            if (getMinThreads != null && setMinThreads != null)
            {
                var threadCounts = new object[2];
                getMinThreads.Invoke(null, threadCounts);


                int workerThreads = (int)threadCounts[0];
                if (workerThreads < minWorkerThreads)
                {
                    threadCounts[0] = minWorkerThreads;
                    setMinThreads.Invoke(null, threadCounts);
                }
            }
        }
    }
}