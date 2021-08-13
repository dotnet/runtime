// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    /// <summary>
    /// A static class for getting information about the Just In Time compiler.
    /// </summary>
    public static partial class JitInfo
    {
        /// <summary>
        /// Get the amount of time the JIT Compiler has spent compiling methods. If <paramref name="currentThread"/> is true,
        /// then this value is scoped to the current thread, otherwise, this is a global value.
        /// </summary>
        /// <param name="currentThread">Whether the returned value should be specific to the current thread. Default: false</param>
        /// <returns>The amount of time the JIT Compiler has spent compiling methods.</returns>
        public static TimeSpan GetCompilationTime(bool currentThread = false)
        {
            // TimeSpan.FromTicks() takes 100ns ticks
            return TimeSpan.FromTicks(GetCompilationTimeInTicks(currentThread));
        }
    }
}