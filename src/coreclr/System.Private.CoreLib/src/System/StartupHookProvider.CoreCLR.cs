// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace System
{
    internal static partial class StartupHookProvider
    {
        private static unsafe void ManagedStartup(char* pDiagnosticStartupHooks)
        {
#if FEATURE_PERFTRACING
            if (EventSource.IsSupported)
                RuntimeEventSource.Initialize();
#endif

            if (IsSupported)
                ProcessStartupHooks(new string(pDiagnosticStartupHooks));
        }
    }
}
