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

        /// <safety>Runtime FCall that updates the GC latency mode from an enum argument and returns a status enum; it accesses no caller-supplied memory.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static safe extern SetLatencyModeStatus SetGCLatencyMode(GCLatencyMode newLatencyMode);

        /// <safety>Runtime FCall that returns the large-object-heap compaction mode as an enum value; it accesses no caller-supplied memory.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static safe extern GCLargeObjectHeapCompactionMode GetLOHCompactionMode();

        /// <safety>Runtime FCall that updates the large-object-heap compaction mode from an enum argument; it accesses no caller-supplied memory.</safety>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static safe extern void SetLOHCompactionMode(GCLargeObjectHeapCompactionMode newLOHCompactionMode);
    }
}
