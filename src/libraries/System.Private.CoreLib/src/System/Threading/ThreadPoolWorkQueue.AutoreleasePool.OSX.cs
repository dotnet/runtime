// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static bool EnableDispatchAutoreleasePool { get; } =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableDispatchAutoreleasePool", false);
    }

    internal sealed partial class ThreadPoolWorkQueue
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DispatchItemWithAutoreleasePool(object workItem, Thread currentThread)
        {
            IntPtr autoreleasePool = Interop.Sys.CreateAutoreleasePool();
            try
            {
#pragma warning disable CS0162 // Unreachable code detected. EnableWorkerTracking may be a constant in some runtimes.
                if (ThreadPool.EnableWorkerTracking)
                {
                    DispatchWorkItemWithWorkerTracking(workItem, currentThread);
                }
                else
                {
                    DispatchWorkItem(workItem, currentThread);
                }
#pragma warning restore CS0162
            }
            finally
            {
                Interop.Sys.DrainAutoreleasePool(autoreleasePool);
            }
        }
    }
}
