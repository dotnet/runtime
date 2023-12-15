// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

internal sealed partial class PortableThreadPool
{
    private static partial class WorkerThread
    {
        private static bool IsIOPending => WebWorkerEventLoop.HasJavaScriptInteropDependents;
    }

    private struct CpuUtilizationReader
    {
#pragma warning disable CA1822
        public double CurrentUtilization => 0.0; // FIXME: can we do better
#pragma warning restore CA1822
    }
}
