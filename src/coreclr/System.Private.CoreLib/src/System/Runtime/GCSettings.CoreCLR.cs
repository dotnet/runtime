// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime
{
    public static partial class GCSettings
    {
        public static extern bool IsServerGC
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern GCLatencyMode GetGCLatencyMode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern SetLatencyModeStatus SetGCLatencyMode(GCLatencyMode newLatencyMode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern GCLargeObjectHeapCompactionMode GetLOHCompactionMode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SetLOHCompactionMode(GCLargeObjectHeapCompactionMode newLOHCompactionMode);
    }
}
