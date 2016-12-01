// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed partial class TlsOverPerCoreLockedStacksArrayPool<T>
    {
        /// <summary>Get an identifier for the current thread to use to index into the stacks.</summary>
        private static int ExecutionId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // On Unix, CurrentProcessorNumber is implemented in terms of sched_getcpu, which
                // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber
                // returns -1.  As a fallback in that case and to spread the threads across the buckets
                // by default, we use the current managed thread ID as a proxy.
                int id = CurrentProcessorNumber;
                if (id < 0) id = Environment.CurrentManagedThreadId;
                return id;
            }
        }
    }
}
