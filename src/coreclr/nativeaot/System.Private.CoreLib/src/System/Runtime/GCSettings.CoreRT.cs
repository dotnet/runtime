// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    public static partial class GCSettings
    {
        public static bool IsServerGC =>
            RuntimeImports.RhIsServerGc();

        private static GCLatencyMode GetGCLatencyMode() =>
            RuntimeImports.RhGetGcLatencyMode();

        private static SetLatencyModeStatus SetGCLatencyMode(GCLatencyMode value) =>
            (SetLatencyModeStatus)RuntimeImports.RhSetGcLatencyMode(value);

        private static GCLargeObjectHeapCompactionMode GetLOHCompactionMode() =>
            (GCLargeObjectHeapCompactionMode)RuntimeImports.RhGetLohCompactionMode();

        private static void SetLOHCompactionMode(GCLargeObjectHeapCompactionMode value) =>
            RuntimeImports.RhSetLohCompactionMode((int)value);
    }
}
