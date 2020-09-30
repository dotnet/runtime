// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace Profiler.Tests
{
    /// <summary>
    /// Used by managed profilees to control the profiler
    /// </summary>
    public static class ProfilerControlHelpers
    {
        public static void AttachProfilerToSelf(Guid profilerGuid, string profilerPath)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            client.AttachProfiler(TimeSpan.MaxValue, profilerGuid, profilerPath, null);
        }

        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            return client.StartEventPipeSession(providers, /* requestRunDown */ false);
        }
    }
}
