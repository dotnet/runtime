// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Runtime
{
    public static partial class JitInfo
    {
        /// <summary>
        /// Get the number of bytes of IL that have been compiled. If <paramref name="currentThread"/> is true,
        /// then this value is scoped to the current thread, otherwise, this is a global value.
        /// </summary>
        /// <param name="currentThread">Whether the returned value should be specific to the current thread. Default: false</param>
        /// <returns>The number of bytes of IL the JIT has compiled.</returns>
        public static long GetCompiledILBytes(bool currentThread = false)
        {
            return currentThread ? 0 : (long)EventPipeInternal.GetRuntimeCounterValue(EventPipeInternal.RuntimeCounters.JIT_IL_BYTES_JITTED);
        }

        /// <summary>
        /// Get the number of methods that have been compiled. If <paramref name="currentThread"/> is true,
        /// then this value is scoped to the current thread, otherwise, this is a global value.
        /// </summary>
        /// <param name="currentThread">Whether the returned value should be specific to the current thread. Default: false</param>
        /// <returns>The number of methods the JIT has compiled.</returns>
        public static long GetCompiledMethodCount(bool currentThread = false)
        {
            return currentThread ? 0 : (long)EventPipeInternal.GetRuntimeCounterValue(EventPipeInternal.RuntimeCounters.JIT_METHODS_JITTED);
        }

        // normalized to 100ns ticks on vm side
        private static long GetCompilationTimeInTicks(bool currentThread = false)
        {
            return currentThread ? 0 : (long)EventPipeInternal.GetRuntimeCounterValue(EventPipeInternal.RuntimeCounters.JIT_TICKS_IN_JIT);
        }
    }
}