// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime
{
    public static partial class GCSettings
    {
        /// <safety>Runtime-implemented FCall getter returning whether server GC is enabled; it reads only runtime configuration and accesses no caller-supplied memory.</safety>
        public static safe extern bool IsServerGC
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <safety>Runtime FCall that returns the current GC latency mode as an enum value; it reads only runtime state and accesses no caller-supplied memory.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static safe extern GCLatencyMode GetGCLatencyMode();

        /// <safety>The runtime stores the argument as the GC's pause mode without validating it, so the caller must
        /// pass a mode the GC defines. <see cref="LatencyMode"/> is the audited entry point that range-checks first.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static unsafe extern SetLatencyModeStatus SetGCLatencyMode(GCLatencyMode newLatencyMode);

        /// <safety>Runtime FCall that returns the large-object-heap compaction mode as an enum value; it accesses no caller-supplied memory.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static safe extern GCLargeObjectHeapCompactionMode GetLOHCompactionMode();

        /// <safety>The GC documents this argument as already verified by CoreLib and stores it without validating it,
        /// so the caller must pass a mode the GC defines. <see cref="LargeObjectHeapCompactionMode"/> is the audited
        /// entry point that range-checks first.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static unsafe extern void SetLOHCompactionMode(GCLargeObjectHeapCompactionMode newLOHCompactionMode);
    }
}
