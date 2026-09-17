// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System;

internal static class GCPauseReporting
{
    internal static bool IsSupported()
    {
#if CORECLR && FEATURE_PERFTRACING
        return EventSource.IsSupported && GC.IsGCPauseReportingSupported();
#else
        return false;
#endif
    }
}
