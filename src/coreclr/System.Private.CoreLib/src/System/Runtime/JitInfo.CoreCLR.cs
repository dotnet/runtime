// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern long GetCompiledILBytes(bool currentThread = false);

        /// <summary>
        /// Get the number of methods that have been compiled. If <paramref name="currentThread"/> is true,
        /// then this value is scoped to the current thread, otherwise, this is a global value.
        /// </summary>
        /// <param name="currentThread">Whether the returned value should be specific to the current thread. Default: false</param>
        /// <returns>The number of methods the JIT has compiled.</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern long GetCompiledMethodCount(bool currentThread = false);

        // Normalized to 100ns ticks on vm side
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long GetCompilationTimeInTicks(bool currentThread = false);
    }
}
